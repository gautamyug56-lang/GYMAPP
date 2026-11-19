using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Exceptions;
using Rackroom.Application.Services;
using Rackroom.Domain.Entities;

namespace Rackroom.Web.Pages.WorkoutPlans;

[Authorize(Policy = "StaffOrAdmin")]
public class AssignModel : PageModel
{
    private readonly IWorkoutPlanManagementService _service;
    private readonly IMemberManagementService _members;
    public AssignModel(IWorkoutPlanManagementService service, IMemberManagementService members){_service=service;_members=members;}
    [BindProperty(SupportsGet=true)] public Guid TemplateId { get; set; }
    [BindProperty] public Guid MemberId { get; set; }
    public IReadOnlyList<Member> Members { get; private set; } = [];
    public string? Error { get; private set; }
    public async Task OnGetAsync(string? search, CancellationToken ct){Members=await _members.SearchMembersAsync(search,ct);}
    public async Task<IActionResult> OnPostAsync(CancellationToken ct){try{await _service.AssignTemplateAsync(TemplateId,MemberId,ct);return RedirectToPage("./Index");}catch(BusinessRuleException ex){Error=ex.Message;Members=await _members.SearchMembersAsync(null,ct);return Page();}}
}
