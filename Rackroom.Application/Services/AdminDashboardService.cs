using Rackroom.Application.Abstractions;
using Rackroom.Application.Exceptions;

namespace Rackroom.Application.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardData> GetDashboardAsync(
        int expiringPage,
        int paymentDeadlinePage,
        int attendancePage,
        CancellationToken cancellationToken = default);
}

public sealed record AdminDashboardData(
    DashboardMetricsData Metrics,
    IReadOnlyList<DashboardChartPoint> RevenueChart,
    IReadOnlyList<DashboardChartPoint> NewMembersChart,
    DashboardPageData<DashboardMembershipRow> ExpiringMemberships,
    DashboardPageData<DashboardMembershipRow> PaymentDeadlines,
    DashboardPageData<DashboardAttendanceRow> TodaysAttendance);

public sealed class AdminDashboardService : IAdminDashboardService
{
    private const int PageSize = 10;

    private readonly ICurrentGymContext _currentGymContext;
    private readonly IRackroomDataStore _dataStore;

    public AdminDashboardService(
        ICurrentGymContext currentGymContext,
        IRackroomDataStore dataStore)
    {
        _currentGymContext = currentGymContext;
        _dataStore = dataStore;
    }

    public async Task<AdminDashboardData> GetDashboardAsync(
        int expiringPage,
        int paymentDeadlinePage,
        int attendancePage,
        CancellationToken cancellationToken = default)
    {
        if (expiringPage < 1 || paymentDeadlinePage < 1 || attendancePage < 1)
        {
            throw new BusinessRuleException("Dashboard page numbers must be at least 1.");
        }

        Guid gymId = _currentGymContext.GymId
            ?? throw new BusinessRuleException("No gym is associated with the current user.");

        DateTime nowUtc = DateTime.UtcNow;
        DateTime monthStartUtc = new(
            nowUtc.Year,
            nowUtc.Month,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc);

        DateTime nextMonthStartUtc = monthStartUtc.AddMonths(1);
        DateTime firstChartMonthUtc = monthStartUtc.AddMonths(-5);

        DashboardMetricsData metrics =
            await _dataStore.GetDashboardMetricsAsync(
                gymId,
                monthStartUtc,
                nextMonthStartUtc,
                nowUtc,
                nowUtc.Date.AddDays(1),
                nowUtc.AddDays(30),
                nowUtc.AddDays(7),
                cancellationToken);

        IReadOnlyList<DashboardChartPoint> revenue =
            await _dataStore.GetRevenueByMonthAsync(
                gymId,
                firstChartMonthUtc,
                nextMonthStartUtc,
                cancellationToken);

        IReadOnlyList<DashboardChartPoint> newMembers =
            await _dataStore.GetNewMembersByMonthAsync(
                gymId,
                firstChartMonthUtc,
                nextMonthStartUtc,
                cancellationToken);

        DashboardPageData<DashboardMembershipRow> expiring =
            await _dataStore.GetExpiringMembershipsAsync(
                gymId,
                nowUtc,
                nowUtc.AddDays(30),
                expiringPage,
                PageSize,
                cancellationToken);

        DashboardPageData<DashboardMembershipRow> paymentDeadlines =
            await _dataStore.GetPaymentDeadlinesAsync(
                gymId,
                nowUtc,
                nowUtc.AddDays(7),
                paymentDeadlinePage,
                PageSize,
                cancellationToken);

        DashboardPageData<DashboardAttendanceRow> attendance =
            await _dataStore.GetTodaysAttendanceAsync(
                gymId,
                nowUtc.Date,
                nowUtc.Date.AddDays(1),
                attendancePage,
                PageSize,
                cancellationToken);

        return new AdminDashboardData(
            metrics,
            FillMissingMonths(revenue, firstChartMonthUtc, 6),
            FillMissingMonths(newMembers, firstChartMonthUtc, 6),
            expiring,
            paymentDeadlines,
            attendance);
    }

    private static IReadOnlyList<DashboardChartPoint> FillMissingMonths(
        IReadOnlyList<DashboardChartPoint> points,
        DateTime firstMonthUtc,
        int count)
    {
        var lookup = points.ToDictionary(x => x.Label, x => x.Value);
        var result = new List<DashboardChartPoint>(count);

        for (int i = 0; i < count; i++)
        {
            DateTime month = firstMonthUtc.AddMonths(i);
            string label = month.ToString("yyyy-MM");

            result.Add(new DashboardChartPoint(
                label,
                lookup.TryGetValue(label, out decimal value) ? value : 0m));
        }

        return result;
    }
}
