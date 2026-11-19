using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.MemberPortal;

[Authorize(Policy = "MemberOnly")]
public class CancelModel : PageModel
{
    private readonly IMemberPortalService _service;
    public CancelModel(IMemberPortalService service) => _service = service;
    [BindProperty(SupportsGet=true)] public Guid MembershipId { get; set; }
    [BindProperty] public string Reason { get; set; } = "";
    public string? Error { get; private set; }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        try { await _service.RequestCancellationAsync(MembershipId, Reason, DateTime.UtcNow, ct); return RedirectToPage("./Index"); }
        catch (BusinessRuleException ex) { Error = ex.Message; return Page(); }
    }
}
