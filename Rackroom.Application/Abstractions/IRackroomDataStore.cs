using Rackroom.Domain.Entities;

namespace Rackroom.Application.Abstractions;

public interface IRackroomDataStore
{
    Task<Member?> GetMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<Member?> GetMemberByEmailAsync(Guid gymId, string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Member>> SearchMembersAsync(Guid gymId, string? search, CancellationToken cancellationToken = default);
    Task<bool> MemberEmailExistsAsync(Guid gymId, string email, Guid? excludedMemberId = null, CancellationToken cancellationToken = default);
    Task AddMemberAsync(Member member, CancellationToken cancellationToken = default);

    Task<MembershipPlan?> GetMembershipPlanAsync(Guid gymId, Guid planId, CancellationToken cancellationToken = default);
    Task<bool> MembershipPlanNameExistsAsync(Guid gymId, string name, Guid? excludedPlanId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MembershipPlan>> GetMembershipPlansAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task AddMembershipPlanAsync(MembershipPlan plan, CancellationToken cancellationToken = default);

    Task<Membership?> GetMembershipAsync(Guid gymId, Guid membershipId, CancellationToken cancellationToken = default);
    Task<Membership?> GetMembershipForPaymentAsync(Guid gymId, Guid membershipId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveMembershipAsync(Guid gymId, Guid memberId, DateTime atUtc, CancellationToken cancellationToken = default);
    Task AddMembershipAsync(Membership membership, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Membership>> GetMembershipsAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<MembershipPause?> GetMembershipPauseAsync(Guid gymId, Guid pauseId, CancellationToken cancellationToken = default);
    Task<int> CountApprovedPausesInCalendarYearAsync(Guid gymId, Guid membershipId, int year, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingPauseAsync(Guid gymId, Guid membershipId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken = default);
    Task AddMembershipPauseAsync(MembershipPause pause, CancellationToken cancellationToken = default);

    Task<bool> CancellationExistsAsync(Guid gymId, Guid membershipId, CancellationToken cancellationToken = default);
    Task AddCancellationRecordAsync(CancellationRecord cancellationRecord, CancellationToken cancellationToken = default);

    Task AddPaymentAndInvoiceAsync(Payment payment, Invoice invoice, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaymentTotalAsync(Guid gymId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task<Membership?> GetActiveMembershipForMemberAsync(Guid gymId, Guid memberId, DateTime atUtc, CancellationToken cancellationToken = default);
    Task AddAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetMemberAttendanceHistoryAsync(Guid gymId, Guid memberId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetGymAttendanceHistoryAsync(Guid gymId, int take, CancellationToken cancellationToken = default);
    Task<AttendanceStatisticsData> GetMemberAttendanceStatisticsAsync(Guid gymId, Guid memberId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<AttendanceStatisticsData> GetGymAttendanceStatisticsAsync(Guid gymId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record AttendanceStatisticsData(
    int TotalCheckIns,
    int CurrentMonthCheckIns,
    int Last30DaysCheckIns,
    int DistinctDaysVisited);
