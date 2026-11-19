using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.Plans;

[Authorize(Policy = "StaffOrAdmin")]
public class CreateModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;

    public CreateModel(IMembershipManagementService membershipService)
    {
        _membershipService = membershipService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IActionResult OnGet()
    {
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
            await _membershipService.CreatePlanAsync(
                Input.Name,
                Input.DurationMonths,
                Input.Price,
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
        public string Name { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(1, 12)]
        public int DurationMonths { get; set; } = 1;

        [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0.01", "100000000")]
        public decimal Price { get; set; }
    }
}
