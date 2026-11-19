using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.WorkoutPlans;

[Authorize(Policy = "StaffOrAdmin")]
public class IndexModel : PageModel
{
    private readonly IWorkoutPlanManagementService _service;
    public IReadOnlyList<WorkoutPlanTemplate> Templates { get; private set; } = [];
    public IndexModel(IWorkoutPlanManagementService service) => _service = service;
    public async Task OnGetAsync(CancellationToken ct) => Templates = await _service.GetTemplatesAsync(ct);
}
