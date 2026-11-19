using Rackroom.Domain.Entities;

namespace Rackroom.Application.Abstractions;

public interface IRackroomDataStore
{
    Task<Member?> GetMemberAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
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

    Task<DashboardMetricsData> GetDashboardMetricsAsync(
        Guid gymId,
        DateTime monthStartUtc,
        DateTime nextMonthStartUtc,
        DateTime nowUtc,
        DateTime tomorrowUtc,
        DateTime expiryWindowEndUtc,
        DateTime paymentDeadlineEndUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardChartPoint>> GetRevenueByMonthAsync(
        Guid gymId,
        DateTime firstMonthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DashboardChartPoint>> GetNewMembersByMonthAsync(
        Guid gymId,
        DateTime firstMonthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default);

    Task<DashboardPageData<DashboardMembershipRow>> GetExpiringMembershipsAsync(
        Guid gymId,
        DateTime nowUtc,
        DateTime endUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<DashboardPageData<DashboardMembershipRow>> GetPaymentDeadlinesAsync(
        Guid gymId,
        DateTime nowUtc,
        DateTime endUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<DashboardPageData<DashboardAttendanceRow>> GetTodaysAttendanceAsync(
        Guid gymId,
        DateTime todayStartUtc,
        DateTime tomorrowUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChurnFeatureRow>> GetActiveChurnFeatureRowsAsync(Guid gymId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task AddChurnPredictionAsync(ChurnPrediction prediction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChurnPrediction>> GetLatestChurnPredictionsAsync(Guid gymId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetGymIdsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Membership>> GetMembershipsForExpiryReminderAsync(
        Guid gymId,
        DateTime nowUtc,
        DateTime reminderWindowEndUtc,
        CancellationToken cancellationToken = default);

    Task<Membership?> GetActiveMembershipForMemberAsync(Guid gymId, Guid memberId, DateTime atUtc, CancellationToken cancellationToken = default);
    Task<Membership?> GetMemberActiveMembershipAsync(Guid gymId, Guid memberId, DateTime atUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetMemberPaymentsAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> GetMemberInvoicesAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<bool> InvoiceBelongsToMemberAsync(Guid gymId, int invoiceNumber, Guid memberId, CancellationToken cancellationToken = default);
    Task<WorkoutPlanAssignment?> GetActiveWorkoutPlanAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BodyMeasurement>> GetBodyMeasurementsAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task<bool> HasMeasurementInCalendarMonthAsync(Guid gymId, Guid memberId, DateTime recordedAtUtc, CancellationToken cancellationToken = default);
    Task AddBodyMeasurementAsync(BodyMeasurement measurement, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkoutPlanTemplate>> GetWorkoutPlanTemplatesAsync(Guid gymId, CancellationToken cancellationToken = default);
    Task<WorkoutPlanTemplate?> GetWorkoutPlanTemplateAsync(Guid gymId, Guid templateId, CancellationToken cancellationToken = default);
    Task<bool> WorkoutPlanTemplateNameExistsAsync(Guid gymId, string name, Guid? excludedTemplateId = null, CancellationToken cancellationToken = default);
    Task AddWorkoutPlanTemplateAsync(WorkoutPlanTemplate template, CancellationToken cancellationToken = default);
    Task<bool> MemberExistsAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default);
    Task EndActiveWorkoutPlanAssignmentsAsync(Guid gymId, Guid memberId, DateTime endedAtUtc, CancellationToken cancellationToken = default);
    Task AddWorkoutPlanAssignmentAsync(WorkoutPlanAssignment assignment, CancellationToken cancellationToken = default);
    Task AddAttendanceAsync(Attendance attendance, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetMemberAttendanceHistoryAsync(Guid gymId, Guid memberId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Attendance>> GetGymAttendanceHistoryAsync(Guid gymId, int take, CancellationToken cancellationToken = default);
    Task<AttendanceStatisticsData> GetMemberAttendanceStatisticsAsync(Guid gymId, Guid memberId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<AttendanceStatisticsData> GetGymAttendanceStatisticsAsync(Guid gymId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardMetricsData(
    decimal MonthlyRevenue,
    int NewMembers,
    int ExpiringMemberships,
    int PaymentDeadlines,
    int TodaysAttendance);

public sealed record DashboardChartPoint(
    string Label,
    decimal Value);

public sealed record DashboardMembershipRow(
    Guid MembershipId,
    string MemberName,
    string Email,
    string PlanName,
    DateTime EndDateUtc);

public sealed record DashboardAttendanceRow(
    Guid AttendanceId,
    string MemberName,
    DateTime CheckedInAtUtc,
    string Source);

public sealed record DashboardPageData<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AttendanceStatisticsData(
    int TotalCheckIns,
    int CurrentMonthCheckIns,
    int Last30DaysCheckIns,
    int DistinctDaysVisited);
