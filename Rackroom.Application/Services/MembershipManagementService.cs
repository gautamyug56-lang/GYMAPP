using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.Services;

public interface IMembershipManagementService
{
    Task<MembershipPlan> CreatePlanAsync(
        string name,
        int durationMonths,
        decimal price,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipPlan>> GetPlansAsync(
        CancellationToken cancellationToken = default);

    Task<Membership> AssignMembershipAsync(
        Guid memberId,
        Guid membershipPlanId,
        DateTime startDateUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Membership>> GetMembershipsAsync(
        CancellationToken cancellationToken = default);

    Task<MembershipPause> RequestPauseAsync(
        Guid membershipId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string? reason,
        CancellationToken cancellationToken = default);

    Task ApprovePauseAsync(
        Guid pauseId,
        Guid approvedByUserId,
        string approverRole,
        DateTime approvedAtUtc,
        CancellationToken cancellationToken = default);

    Task CancelMembershipAsync(
        Guid membershipId,
        string reason,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed class MembershipManagementService : IMembershipManagementService
{
    private const int MaxPauseDays = 30;
    private const int MaxPausesPerCalendarYear = 2;

    private readonly ICurrentGymContext _currentGymContext;
    private readonly IRackroomDataStore _dataStore;

    public MembershipManagementService(
        ICurrentGymContext currentGymContext,
        IRackroomDataStore dataStore)
    {
        _currentGymContext = currentGymContext;
        _dataStore = dataStore;
    }

    public async Task<MembershipPlan> CreatePlanAsync(
        string name,
        int durationMonths,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();
        string normalizedName = NormalizeRequired(name, "Plan name");

        if (durationMonths is not (1 or 3 or 12))
        {
            throw new BusinessRuleException(
                "Plan duration must be 1 month, 3 months, or 12 months.");
        }

        if (price <= 0)
        {
            throw new BusinessRuleException("Plan price must be greater than zero.");
        }

        if (await _dataStore.MembershipPlanNameExistsAsync(
                gymId,
                normalizedName,
                null,
                cancellationToken))
        {
            throw new BusinessRuleException("A membership plan with this name already exists.");
        }

        MembershipPlan plan = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = normalizedName,
            DurationMonths = durationMonths,
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddMembershipPlanAsync(plan, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);

        return plan;
    }

    public Task<IReadOnlyList<MembershipPlan>> GetPlansAsync(
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetMembershipPlansAsync(GetGymId(), cancellationToken);
    }

    public Task<IReadOnlyList<Membership>> GetMembershipsAsync(
        CancellationToken cancellationToken = default)
    {
        return _dataStore.GetMembershipsAsync(GetGymId(), cancellationToken);
    }

    public async Task<Membership> AssignMembershipAsync(
        Guid memberId,
        Guid membershipPlanId,
        DateTime startDateUtc,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (startDateUtc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleException("Membership start date must be UTC.");
        }

        Member member = await _dataStore.GetMemberAsync(gymId, memberId, cancellationToken)
            ?? throw new BusinessRuleException("Member was not found in the current gym.");

        if (!member.IsActive)
        {
            throw new BusinessRuleException("An inactive member cannot receive a new membership.");
        }

        MembershipPlan plan = await _dataStore.GetMembershipPlanAsync(
                gymId,
                membershipPlanId,
                cancellationToken)
            ?? throw new BusinessRuleException("Membership plan was not found in the current gym.");

        if (!plan.IsActive)
        {
            throw new BusinessRuleException("An inactive membership plan cannot be assigned.");
        }

        if (await _dataStore.HasActiveMembershipAsync(
                gymId,
                memberId,
                startDateUtc,
                cancellationToken))
        {
            throw new BusinessRuleException("The member already has an active membership.");
        }

        DateTime endDateUtc = startDateUtc.AddMonths(plan.DurationMonths);

        Membership membership = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MemberId = memberId,
            MembershipPlanId = membershipPlanId,
            StartDateUtc = startDateUtc,
            EndDateUtc = endDateUtc,
            Status = MembershipStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddMembershipAsync(membership, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);

        return membership;
    }

    public async Task<MembershipPause> RequestPauseAsync(
        Guid membershipId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();
        Membership membership = await RequireMembershipAsync(
            gymId,
            membershipId,
            cancellationToken);

        ValidatePauseDates(startDateUtc, endDateUtc, membership);

        if (await _dataStore.HasOverlappingPauseAsync(
                gymId,
                membershipId,
                startDateUtc,
                endDateUtc,
                cancellationToken))
        {
            throw new BusinessRuleException("The requested pause overlaps another pause.");
        }

        MembershipPause pause = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MembershipId = membershipId,
            StartDateUtc = startDateUtc,
            EndDateUtc = endDateUtc,
            PausedDays = CalculatePauseDays(startDateUtc, endDateUtc),
            Status = PauseStatus.Pending,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await _dataStore.AddMembershipPauseAsync(pause, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);

        return pause;
    }

    public async Task ApprovePauseAsync(
        Guid pauseId,
        Guid approvedByUserId,
        string approverRole,
        DateTime approvedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();

        if (approvedByUserId == Guid.Empty)
        {
            throw new BusinessRuleException("A staff approver is required.");
        }

        if (!string.Equals(approverRole, "Staff", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(approverRole, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("Only staff or an admin can approve a membership pause.");
        }

        if (approvedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleException("Approval time must be UTC.");
        }

        MembershipPause pause = await _dataStore.GetMembershipPauseAsync(
                gymId,
                pauseId,
                cancellationToken)
            ?? throw new BusinessRuleException("Membership pause was not found in the current gym.");

        if (pause.Status != PauseStatus.Pending)
        {
            throw new BusinessRuleException("Only a pending pause can be approved.");
        }

        Membership membership = await RequireMembershipAsync(
            gymId,
            pause.MembershipId,
            cancellationToken);

        int approvedPausesThisYear =
            await _dataStore.CountApprovedPausesInCalendarYearAsync(
                gymId,
                pause.MembershipId,
                pause.StartDateUtc.Year,
                cancellationToken);

        if (approvedPausesThisYear >= MaxPausesPerCalendarYear)
        {
            throw new BusinessRuleException(
                "A member can have at most two approved pauses per calendar year.");
        }

        ValidatePauseDates(pause.StartDateUtc, pause.EndDateUtc, membership);

        pause.Status = PauseStatus.Approved;
        pause.ApprovedByUserId = approvedByUserId;
        pause.ApprovedAtUtc = approvedAtUtc;

        // Approval extends the paid membership by the exact number of paused days.
        membership.EndDateUtc = membership.EndDateUtc.AddDays(pause.PausedDays);

        await _dataStore.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelMembershipAsync(
        Guid membershipId,
        string reason,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();
        Membership membership = await RequireMembershipAsync(
            gymId,
            membershipId,
            cancellationToken);

        if (requestedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleException("Cancellation request time must be UTC.");
        }

        string normalizedReason = NormalizeRequired(reason, "Cancellation reason");

        if (await _dataStore.CancellationExistsAsync(
                gymId,
                membershipId,
                cancellationToken))
        {
            throw new BusinessRuleException("This membership already has a cancellation request.");
        }

        if (membership.Status == MembershipStatus.Cancelled)
        {
            throw new BusinessRuleException("This membership is already cancelled.");
        }

        CancellationRecord record = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MembershipId = membershipId,
            RequestedAtUtc = requestedAtUtc,
            EffectiveAtUtc = membership.EndDateUtc,
            Reason = normalizedReason
        };

        await _dataStore.AddCancellationRecordAsync(record, cancellationToken);
        await _dataStore.SaveChangesAsync(cancellationToken);
    }

    private async Task<Membership> RequireMembershipAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        return await _dataStore.GetMembershipAsync(gymId, membershipId, cancellationToken)
            ?? throw new BusinessRuleException("Membership was not found in the current gym.");
    }

    private static void ValidatePauseDates(
        DateTime startDateUtc,
        DateTime endDateUtc,
        Membership membership)
    {
        if (startDateUtc.Kind != DateTimeKind.Utc ||
            endDateUtc.Kind != DateTimeKind.Utc)
        {
            throw new BusinessRuleException("Pause dates must be UTC.");
        }

        if (endDateUtc < startDateUtc)
        {
            throw new BusinessRuleException("Pause end date cannot be before its start date.");
        }

        if (startDateUtc < membership.StartDateUtc ||
            endDateUtc > membership.EndDateUtc)
        {
            throw new BusinessRuleException(
                "A pause must be completely inside the current membership period.");
        }

        if (CalculatePauseDays(startDateUtc, endDateUtc) > MaxPauseDays)
        {
            throw new BusinessRuleException("A membership pause cannot exceed 30 days.");
        }
    }

    private static int CalculatePauseDays(DateTime startDateUtc, DateTime endDateUtc)
    {
        // Dates are treated as inclusive calendar dates for the pause rule.
        return (endDateUtc.Date - startDateUtc.Date).Days + 1;
    }

    private Guid GetGymId()
    {
        return _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        string result = value.Trim();

        if (string.IsNullOrWhiteSpace(result))
        {
            throw new BusinessRuleException($"{fieldName} is required.");
        }

        return result;
    }
}
