using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.Admin;

[Authorize(Policy = "AdminOnly")]
public class DashboardModel : PageModel
{
    private readonly IAdminDashboardService _dashboardService;

    public DashboardModel(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public AdminDashboardData Data { get; private set; } =
        new(
            new(0m, 0, 0, 0, 0),
            [],
            [],
            new([], 0, 1, 10),
            new([], 0, 1, 10),
            new([], 0, 1, 10));

    public int ExpiringPage { get; private set; }
    public int PaymentDeadlinePage { get; private set; }
    public int AttendancePage { get; private set; }

    public async Task OnGetAsync(
        int expiringPage = 1,
        int paymentDeadlinePage = 1,
        int attendancePage = 1,
        CancellationToken cancellationToken = default)
    {
        ExpiringPage = Math.Max(1, expiringPage);
        PaymentDeadlinePage = Math.Max(1, paymentDeadlinePage);
        AttendancePage = Math.Max(1, attendancePage);

        Data = await _dashboardService.GetDashboardAsync(
            ExpiringPage,
            PaymentDeadlinePage,
            AttendancePage,
            cancellationToken);
    }

    public int ExpiringPageCount => PageCount(Data.ExpiringMemberships.TotalCount, Data.ExpiringMemberships.PageSize);
    public DashboardPaginationModel ExpiringPagination => new(ExpiringPage, ExpiringPageCount, "expiringPage");
    public DashboardPaginationModel PaymentDeadlinePagination => new(PaymentDeadlinePage, PaymentDeadlinePageCount, "paymentDeadlinePage");
    public DashboardPaginationModel AttendancePagination => new(AttendancePage, AttendancePageCount, "attendancePage");
    public int PaymentDeadlinePageCount => PageCount(Data.PaymentDeadlines.TotalCount, Data.PaymentDeadlines.PageSize);
    public int AttendancePageCount => PageCount(Data.TodaysAttendance.TotalCount, Data.TodaysAttendance.PageSize);
    public string RevenueJson => ToJson(Data.RevenueChart);
    public string NewMembersJson => ToJson(Data.NewMembersChart);

    private static string ToJson<T>(T value) =>
        JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

    private static int PageCount(int totalCount, int pageSize) =>
        Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
}
