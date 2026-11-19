using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Memberships;

[Authorize(Policy = "StaffOrAdmin")]
public class CancelModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;

    public CancelModel(IMembershipManagementService membershipService)
    {
        _membershipService = membershipService;
    }

    public Membership? Membership { get; private set; }

    [BindProperty]
    public string Reason { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Membership = (await _membershipService.GetMembershipsAsync(cancellationToken))
            .SingleOrDefault(x => x.Id == id);

        return Membership is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _membershipService.CancelMembershipAsync(
                id,
                Reason,
                DateTime.UtcNow,
                cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            Membership = (await _membershipService.GetMembershipsAsync(cancellationToken))
                .SingleOrDefault(x => x.Id == id);
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
