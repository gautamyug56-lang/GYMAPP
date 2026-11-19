using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Memberships;

[Authorize(Policy = "StaffOrAdmin")]
public class IndexModel : PageModel
{
    private readonly IMembershipManagementService _membershipService;

    public IndexModel(IMembershipManagementService membershipService)
    {
        _membershipService = membershipService;
    }

    public IReadOnlyList<Membership> Memberships { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Memberships = await _membershipService.GetMembershipsAsync(cancellationToken);
    }
}
