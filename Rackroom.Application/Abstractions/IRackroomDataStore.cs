using Rackroom.Domain.Entities;

namespace Rackroom.Application.Abstractions;

public interface IRackroomDataStore
{
    Task<Member?> GetMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Member>> SearchMembersAsync(
        Guid gymId,
        string? search,
        CancellationToken cancellationToken = default);
    Task<bool> MemberEmailExistsAsync(
        Guid gymId,
        string email,
        Guid? excludedMemberId = null,
        CancellationToken cancellationToken = default);
    Task AddMemberAsync(Member member, CancellationToken cancellationToken = default);

    Task<MembershipPlan?> GetMembershipPlanAsync(
        Guid gymId,
        Guid planId,
        CancellationToken cancellationToken = default);
    Task<bool> MembershipPlanNameExistsAsync(
        Guid gymId,
        string name,
        Guid? excludedPlanId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(
        Guid gymId,
        CancellationToken cancellationToken = default);
    Task AddMembershipPlanAsync(
        MembershipPlan plan,
        CancellationToken cancellationToken = default);

    Task<Membership?> GetMembershipAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveMembershipAsync(
        Guid gymId,
        Guid memberId,
        DateTime atUtc,
        CancellationToken cancellationToken = default);
    Task AddMembershipAsync(
        Membership membership,
        CancellationToken cancellationToken = default);

    Task<MembershipPause?> GetMembershipPauseAsync(
        Guid gymId,
        Guid pauseId,
        CancellationToken cancellationToken = default);
    Task<int> CountApprovedPausesInCalendarYearAsync(
        Guid gymId,
        Guid membershipId,
        int year,
        CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingPauseAsync(
        Guid gymId,
        Guid membershipId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken = default);
    Task AddMembershipPauseAsync(
        MembershipPause pause,
        CancellationToken cancellationToken = default);

    Task<bool> CancellationExistsAsync(
        Guid gymId,
        Guid membershipId,
        CancellationToken cancellationToken = default);
    Task AddCancellationRecordAsync(
        CancellationRecord cancellationRecord,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
