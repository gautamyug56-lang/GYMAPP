using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Members;

[Authorize(Policy = "StaffOrAdmin")]
public class EditModel : PageModel
{
    private readonly IMemberManagementService _memberService;

    public EditModel(IMemberManagementService memberService)
    {
        _memberService = memberService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool IsActive { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        Member? member = await _memberService.GetMemberAsync(id, cancellationToken);

        if (member is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Id = member.Id,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email
        };
        IsActive = member.IsActive;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _memberService.EditMemberAsync(
                Input.Id,
                Input.FirstName,
                Input.LastName,
                Input.Email,
                cancellationToken);
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }

        return RedirectToPage("./Index");
    }

    public sealed class InputModel
    {
        public Guid Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        [System.ComponentModel.DataAnnotations.StringLength(320)]
        public string Email { get; set; } = string.Empty;
    }
}
