using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.Members;

[Authorize(Policy = "StaffOrAdmin")]
public class CreateModel : PageModel
{
    private readonly IMemberManagementService _memberService;

    public CreateModel(IMemberManagementService memberService)
    {
        _memberService = memberService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _memberService.AddMemberAsync(
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
