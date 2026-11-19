using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.WorkoutPlans;

[Authorize(Policy = "StaffOrAdmin")]
public class CreateModel : PageModel
{
    private readonly IWorkoutPlanManagementService _service;
    public CreateModel(IWorkoutPlanManagementService service) => _service = service;
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public string PlanJson { get; set; } = """{"days":[{"name":"Push","exercises":["Bench Press"]},{"name":"Pull","exercises":["Lat Pulldown"]},{"name":"Legs","exercises":["Squat"]}]}""";
    public string? Error { get; private set; }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct){try{await _service.CreateTemplateAsync(Name,Description,PlanJson,ct);return RedirectToPage("./Index");}catch(BusinessRuleException ex){Error=ex.Message;return Page();}}
}
