using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.Members;

[Authorize(Policy = "StaffOrAdmin")]
public class IndexModel : PageModel
{
    private readonly IMemberManagementService _memberService;

    public IndexModel(IMemberManagementService memberService)
    {
        _memberService = memberService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public IReadOnlyList<Member> Members { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Members = await _memberService.SearchMembersAsync(Search, cancellationToken);
    }
}
