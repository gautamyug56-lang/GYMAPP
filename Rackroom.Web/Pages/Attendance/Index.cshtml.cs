using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Attendance;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IAttendanceManagementService _attendanceService;

    public IndexModel(IAttendanceManagementService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    public IReadOnlyList<Attendance> AttendanceHistory { get; private set; } = [];
    public AttendanceStatistics Statistics { get; private set; } =
        new(0, 0, 0, 0);

    public bool IsStaffView => User.IsInRole("Staff") || User.IsInRole("Admin");

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (IsStaffView)
        {
            AttendanceHistory = await _attendanceService.GetGymHistoryAsync(100, cancellationToken);
            Statistics = await _attendanceService.GetGymStatisticsAsync(cancellationToken);
            return;
        }

        string? memberIdValue = User.FindFirstValue("MemberId");

        if (!Guid.TryParse(memberIdValue, out Guid memberId))
            return;

        AttendanceHistory = await _attendanceService.GetMemberHistoryAsync(
            memberId,
            50,
            cancellationToken);

        Statistics = await _attendanceService.GetMemberStatisticsAsync(
            memberId,
            cancellationToken);
    }
}
