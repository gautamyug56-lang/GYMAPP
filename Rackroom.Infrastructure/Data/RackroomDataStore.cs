using Microsoft.EntityFrameworkCore;
using Rackroom.Application.Abstractions;
using Rackroom.Domain.Entities;

namespace Rackroom.Infrastructure.Data;

public sealed class RackroomDataStore : IRackroomDataStore
{
    private readonly ApplicationDbContext _db;

    public RackroomDataStore(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Member?> GetMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return _db.Members
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == memberId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Member>> SearchMembersAsync(
        Guid gymId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Member> query = _db.Members
            .AsNoTracking()
            .Where(x => x.GymId == gymId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLower();

            query = query.Where(x =>
                x.FirstName.ToLower().Contains(term) ||
                x.LastName.ToLower().Contains(term) ||
                x.Email.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> MemberEmailExistsAsync(
        Guid gymId,
        string email,
        Guid? excludedMemberId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.Members.AnyAsync(
            x => x.GymId == gymId &&
                 x.Email == email &&
                 (!excludedMemberId.HasValue || x.Id != excludedMemberId.Value),
            cancellationToken);
    }

    public Task AddMemberAsync(
        Member member,
        CancellationToken cancellationToken = default)
    {
        return _db.Members.AddAsync(member, cancellationToken).AsTask();
    }

    public Task<MembershipPlan?> GetMembershipPlanAsync(
        Guid gymId,
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPlans
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == planId,
                cancellationToken);
    }

    public Task<bool> MembershipPlanNameExistsAsync(
        Guid gymId,
        string name,
        Guid? excludedPlanId = null,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPlans.AnyAsync(
            x => x.GymId == gymId &&
                 x.Name == name &&
                 (!excludedPlanId.HasValue || x.Id != excludedPlanId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _db.MembershipPlans
            .AsNoTracking()
            .Where(x => x.GymId == gymId)
            .OrderBy(x => x.DurationMonths)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddMembershipPlanAsync(
        MembershipPlan plan,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPlans.AddAsync(plan, cancellationToken).AsTask();
    }

    public Task<Membership?> GetMembershipAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships
            .Include(x => x.Pauses)
            .Include(x => x.CancellationRecord)
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == membershipId,
                cancellationToken);
    }

    public Task<bool> HasActiveMembershipAsync(
        Guid gymId,
        Guid memberId,
        DateTime atUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships.AnyAsync(
            x => x.GymId == gymId &&
                 x.MemberId == memberId &&
                 x.Status == MembershipStatus.Active &&
                 x.StartDateUtc <= atUtc &&
                 x.EndDateUtc > atUtc,
            cancellationToken);
    }

    public Task AddMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken = default)
    {
        return _db.Memberships.AddAsync(membership, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<Membership>> GetMembershipsAsync(
        Guid gymId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Memberships
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.MembershipPlan)
            .Include(x => x.Pauses)
            .Include(x => x.CancellationRecord)
            .Where(x => x.GymId == gymId)
            .OrderByDescending(x => x.StartDateUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<MembershipPause?> GetMembershipPauseAsync(
        Guid gymId,
        Guid pauseId,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPauses
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.Id == pauseId,
                cancellationToken);
    }

    public Task<int> CountApprovedPausesInCalendarYearAsync(
        Guid gymId,
        Guid membershipId,
        int year,
        CancellationToken cancellationToken = default)
    {
        DateTime yearStart = new(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime nextYearStart = yearStart.AddYears(1);

        return _db.MembershipPauses.CountAsync(
            x => x.GymId == gymId &&
                 x.MembershipId == membershipId &&
                 x.Status == PauseStatus.Approved &&
                 x.StartDateUtc >= yearStart &&
                 x.StartDateUtc < nextYearStart,
            cancellationToken);
    }

    public Task<bool> HasOverlappingPauseAsync(
        Guid gymId,
        Guid membershipId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPauses.AnyAsync(
            x => x.GymId == gymId &&
                 x.MembershipId == membershipId &&
                 x.Status != PauseStatus.Rejected &&
                 x.StartDateUtc <= endDateUtc &&
                 x.EndDateUtc >= startDateUtc,
            cancellationToken);
    }

    public Task AddMembershipPauseAsync(
        MembershipPause pause,
        CancellationToken cancellationToken = default)
    {
        return _db.MembershipPauses.AddAsync(pause, cancellationToken).AsTask();
    }

    public Task<bool> CancellationExistsAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        return _db.CancellationRecords.AnyAsync(
            x => x.GymId == gymId && x.MembershipId == membershipId,
            cancellationToken);
    }

    public Task AddCancellationRecordAsync(
        CancellationRecord cancellationRecord,
        CancellationToken cancellationToken = default)
    {
        return _db.CancellationRecords.AddAsync(cancellationRecord, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
