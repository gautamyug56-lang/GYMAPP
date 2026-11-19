using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Memberships;

[Authorize(Policy = "StaffOrAdmin")]
public class PauseModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;

    public PauseModel(IMembershipManagementService membershipService)
    {
        _membershipService = membershipService;
    }

    public Membership? Membership { get; private set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Membership> memberships =
            await _membershipService.GetMembershipsAsync(cancellationToken);
        Membership? membership = memberships.SingleOrDefault(x => x.Id == id);

        if (membership is null)
        {
            return NotFound();
        }

        Membership = membership;
        Input.StartDateUtc = DateTime.UtcNow.Date;
        Input.EndDateUtc = DateTime.UtcNow.Date.AddDays(6);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _membershipService.RequestPauseAsync(
                id,
                Input.StartDateUtc,
                Input.EndDateUtc,
                Input.Reason,
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

    public sealed class InputModel
    {
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }

        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string? Reason { get; set; }
    }
}
