using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Memberships;

[Authorize(Policy = "StaffOrAdmin")]
public class CreateModel : PageModel
{
    private readonly IMemberManagementService _memberService;
    private readonly IMembershipManagementService _membershipService;

    public CreateModel(
        IMemberManagementService memberService,
        IMembershipManagementService membershipService)
    {
        _memberService = memberService;
        _membershipService = membershipService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Member> Members { get; private set; } = [];
    public IReadOnlyList<MembershipPlan> Plans { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _membershipService.AssignMembershipAsync(
                Input.MemberId,
                Input.MembershipPlanId,
                DateTime.SpecifyKind(Input.StartDateUtc.Date, DateTimeKind.Utc),
                cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync(CancellationToken cancellationToken)
    {
        Members = (await _memberService.SearchMembersAsync(null, cancellationToken))
            .Where(x => x.IsActive)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToList();

        Plans = (await _membershipService.GetPlansAsync(cancellationToken))
            .Where(x => x.IsActive)
            .ToList();
    }

    public sealed class InputModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public Guid MemberId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public Guid MembershipPlanId { get; set; }

        public DateTime StartDateUtc { get; set; } =
            DateTime.UtcNow.Date;
    }
}
