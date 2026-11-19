using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rackroom.Application.ML;

namespace Rackroom.Web.Pages.Admin.Churn;

[Authorize(Policy = "AdminOnly")]
public class TrainModel : PageModel
{
    private readonly IChurnModelTrainer _trainer;
    public ChurnTrainingResult? Result { get; private set; }
    public TrainModel(IChurnModelTrainer trainer) => _trainer = trainer;
    public async Task<IActionResult> OnPostAsync(CancellationToken ct) { Result = await _trainer.TrainAsync(ct); return Page(); }
}