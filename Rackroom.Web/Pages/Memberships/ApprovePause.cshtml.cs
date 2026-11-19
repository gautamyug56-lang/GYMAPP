using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Memberships;

[Authorize(Policy = "StaffOrAdmin")]
public class ApprovePauseModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;

    public ApprovePauseModel(IMembershipManagementService membershipService)
    {
        _membershipService = membershipService;
    }

    public MembershipPause? Pause { get; private set; }
    public Membership? Membership { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Membership> memberships =
            await _membershipService.GetMembershipsAsync(cancellationToken);

        Pause = memberships
            .SelectMany(x => x.Pauses)
            .SingleOrDefault(x => x.Id == id);

        Membership = memberships.SingleOrDefault(x => x.Id == Pause?.MembershipId);

        return Pause is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid approverId))
        {
            ModelState.AddModelError(string.Empty, "The signed-in user ID is not a valid approver ID.");
            return Page();
        }

        try
        {
            string role = User.IsInRole("Admin") ? "Admin" : "Staff";

            await _membershipService.ApprovePauseAsync(
                id,
                approverId,
                role,
                DateTime.UtcNow,
                cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
