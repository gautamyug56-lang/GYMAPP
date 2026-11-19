using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Plans;

[Authorize(Policy = "StaffOrAdmin")]
public class IndexModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;

    public IndexModel(IMembershipManagementService membershipService)
    {
        _membershipService = membershipService;
    }

    public IReadOnlyList<MembershipPlan> Plans { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Plans = await _membershipService.GetPlansAsync(cancellationToken);
    }
}
