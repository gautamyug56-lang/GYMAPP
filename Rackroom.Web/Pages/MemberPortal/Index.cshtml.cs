using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;

namespace Rackroom.Web.Pages.MemberPortal;

[Authorize(Policy = "MemberOnly")]
public class IndexModel : PageModel
{
    private readonly IMemberPortalService _service;
    public MemberPortalData Data { get; private set; } = default!;

    public IndexModel(IMemberPortalService service) => _service = service;

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => Data = await _service.GetPortalAsync(cancellationToken);
}
