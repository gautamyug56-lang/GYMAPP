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

    public static string ToJson<T>(T value)
    {
        return JsonSerializer.Serialize(value);
    }

    public static int PageCount(int totalCount, int pageSize)
    {
        return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
