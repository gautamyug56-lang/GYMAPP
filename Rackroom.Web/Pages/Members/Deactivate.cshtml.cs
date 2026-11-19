using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Members;

[Authorize(Policy = "StaffOrAdmin")]
public class DeactivateModel : PageModel
{
    private readonly IMemberManagementService _memberService;

    public DeactivateModel(IMemberManagementService memberService)
    {
        _memberService = memberService;
    }

    public Member? Member { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Member = await _memberService.GetMemberAsync(id, cancellationToken);

        return Member is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _memberService.DeactivateMemberAsync(id, cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            Member = await _memberService.GetMemberAsync(id, cancellationToken);
            return Page();
        }

        return RedirectToPage("./Index");
    }
}
