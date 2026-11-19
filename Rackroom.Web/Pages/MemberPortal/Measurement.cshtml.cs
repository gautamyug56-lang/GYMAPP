using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.MemberPortal;

[Authorize(Policy = "MemberOnly")]
public class MeasurementModel : PageModel
{
    private readonly IMemberPortalService _service;
    public MeasurementModel(IMemberPortalService service) => _service = service;
    [BindProperty] public decimal WeightKg { get; set; }
    [BindProperty] public decimal HeightCm { get; set; }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        try { await _service.RecordMonthlyMeasurementAsync(WeightKg, HeightCm, DateTime.UtcNow, ct); return RedirectToPage("./Index"); }
        catch (BusinessRuleException ex) { TempData["Error"] = ex.Message; return RedirectToPage("./Index"); }
    }
}
