using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.Abstractions;

namespace Rackroom.Web.Pages.Admin.Churn;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ICurrentGymContext _context;
    private readonly IRackroomDataStore _store;
    public IReadOnlyList<Rackroom.Domain.Entities.ChurnPrediction> Predictions { get; private set; } = [];
    public IndexModel(ICurrentGymContext context, IRackroomDataStore store) { _context = context; _store = store; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        Guid gymId = _context.GymId ?? Guid.Empty;
        Predictions = gymId == Guid.Empty ? [] : await _store.GetLatestChurnPredictionsAsync(gymId, ct);
    }
}