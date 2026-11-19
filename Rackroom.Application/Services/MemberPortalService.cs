using System.Text.Json;
using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.Services;

public interface IMemberPortalService
{
    Task<MemberPortalData> GetPortalAsync(CancellationToken cancellationToken = default);
    Task<MembershipPause> RequestPauseAsync(Guid membershipId, DateTime startDateUtc, DateTime endDateUtc, string? reason, CancellationToken cancellationToken = default);
    Task RequestCancellationAsync(Guid membershipId, string reason, DateTime requestedAtUtc, CancellationToken cancellationToken = default);
    Task<BodyMeasurement> RecordMonthlyMeasurementAsync(decimal weightKg, decimal heightCm, DateTime recordedAtUtc, CancellationToken cancellationToken = default);
}

public sealed record MemberPortalData(
    Member Member,
    Membership? ActiveMembership,
    IReadOnlyList<Payment> Payments,
    IReadOnlyList<Invoice> Invoices,
    WorkoutPlanAssignment? ActiveWorkoutPlan,
    IReadOnlyList<BodyMeasurement> Measurements,
    IReadOnlyList<Attendance> RecentAttendance);

public sealed class MemberPortalService : IMemberPortalService
{
    private readonly ICurrentGymContext _gymContext;
    private readonly IRackroomDataStore _store;

    public MemberPortalService(ICurrentGymContext gymContext, IRackroomDataStore store)
    {
        _gymContext = gymContext;
        _store = store;
    }

    public async Task<MemberPortalData> GetPortalAsync(CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();
        Guid memberId = GetMemberId();

        Member member = await _store.GetMemberAsync(gymId, memberId, cancellationToken)
            ?? throw new BusinessRuleException("Your member profile could not be found.");

        return new MemberPortalData(
            member,
            await _store.GetMemberActiveMembershipAsync(gymId, memberId, DateTime.UtcNow, cancellationToken),
            await _store.GetMemberPaymentsAsync(gymId, memberId, cancellationToken),
            await _store.GetMemberInvoicesAsync(gymId, memberId, cancellationToken),
            await _store.GetActiveWorkoutPlanAsync(gymId, memberId, cancellationToken),
            await _store.GetBodyMeasurementsAsync(gymId, memberId, cancellationToken),
            await _store.GetMemberAttendanceHistoryAsync(gymId, memberId, 10, cancellationToken));
    }

    public Task<MembershipPause> RequestPauseAsync(
        Guid membershipId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        EnsureOwnMembership(membershipId);
        return new MembershipManagementService(_gymContext, _store)
            .RequestPauseAsync(membershipId, startDateUtc, endDateUtc, reason, cancellationToken);
    }

    public Task RequestCancellationAsync(
        Guid membershipId,
        string reason,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureOwnMembership(membershipId);
        return new MembershipManagementService(_gymContext, _store)
            .CancelMembershipAsync(membershipId, reason, requestedAtUtc, cancellationToken);
    }

    public async Task<BodyMeasurement> RecordMonthlyMeasurementAsync(
        decimal weightKg,
        decimal heightCm,
        DateTime recordedAtUtc,
        CancellationToken cancellationToken = default)
    {
        Guid gymId = GetGymId();
        Guid memberId = GetMemberId();

        if (recordedAtUtc.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("Measurement time must be UTC.");

        if (weightKg <= 0 || weightKg > 500)
            throw new BusinessRuleException("Weight must be greater than 0 and at most 500 kg.");

        if (heightCm <= 0 || heightCm > 300)
            throw new BusinessRuleException("Height must be greater than 0 and at most 300 cm.");

        if (await _store.HasMeasurementInCalendarMonthAsync(
                gymId, memberId, recordedAtUtc, cancellationToken))
        {
            throw new BusinessRuleException(
                "You have already recorded a measurement this calendar month.");
        }

        BodyMeasurement measurement = new()
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            MemberId = memberId,
            RecordedAtUtc = recordedAtUtc,
            WeightKg = decimal.Round(weightKg, 2, MidpointRounding.AwayFromZero),
            HeightCm = decimal.Round(heightCm, 2, MidpointRounding.AwayFromZero)
        };

        await _store.AddBodyMeasurementAsync(measurement, cancellationToken);
        await _store.SaveChangesAsync(cancellationToken);

        return measurement;
    }

    private void EnsureOwnMembership(Guid membershipId)
    {
        if (membershipId == Guid.Empty)
            throw new BusinessRuleException("A membership is required.");
    }

    private Guid GetGymId() =>
        _gymContext.GymId ??
        throw new BusinessRuleException("No gym is associated with the current user.");

    private Guid GetMemberId() =>
        _gymContext.MemberId ??
        throw new BusinessRuleException("No member profile is associated with the current user.");
}
