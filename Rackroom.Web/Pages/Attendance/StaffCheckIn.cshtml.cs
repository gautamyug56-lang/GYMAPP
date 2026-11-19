using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Attendance;

[Authorize(Policy = "StaffOrAdmin")]
public class StaffCheckInModel : PageModel
{
    private readonly IAttendanceManagementService _attendanceService;
    private readonly IMemberManagementService _memberService;

    public StaffCheckInModel(
        IAttendanceManagementService attendanceService,
        IMemberManagementService memberService)
    {
        _attendanceService = attendanceService;
        _memberService = memberService;
    }

    [BindProperty]
    public Guid MemberId { get; set; }

    public IReadOnlyList<Member> Members { get; private set; } = [];

    public string? ErrorMessage { get; private set; }
    public bool CheckedIn { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadMembersAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadMembersAsync(cancellationToken);

        if (MemberId == Guid.Empty)
        {
            ErrorMessage = "Please select a member.";
            return Page();
        }

        try
        {
            await _attendanceService.StaffCheckInAsync(
                MemberId,
                DateTime.UtcNow,
                cancellationToken);

            CheckedIn = true;
        }
        catch (BusinessRuleException exception)
        {
            ErrorMessage = exception.Message;
        }

        return Page();
    }

    private async Task LoadMembersAsync(CancellationToken cancellationToken)
    {
        Members = await _memberService.SearchMembersAsync(null, cancellationToken);
    }
}
