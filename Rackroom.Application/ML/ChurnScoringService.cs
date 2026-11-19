using Microsoft.ML;
using Rackroom.Application.Abstractions;
using Rackroom.Domain.Entities;

namespace Rackroom.Application.ML;

public interface IChurnScoringService
{
    Task<int> ScoreGymAsync(Guid gymId, CancellationToken cancellationToken = default);
}

public sealed class ChurnScoringService : IChurnScoringService
{
    private readonly IRackroomDataStore _store;
    private readonly IChurnModelStore _modelStore;

    public ChurnScoringService(IRackroomDataStore store, IChurnModelStore modelStore)
    {
        _store = store;
        _modelStore = modelStore;
    }

    public async Task<int> ScoreGymAsync(Guid gymId, CancellationToken cancellationToken = default)
    {
        (ITransformer model, _) = await _modelStore.LoadModelAsync(cancellationToken);
        MLContext ml = new(seed: 20261119);
        using PredictionEngine<ChurnData, ChurnPredictionOutput> engine =
            ml.Model.CreatePredictionEngine<ChurnData, ChurnPredictionOutput>(model);

        IReadOnlyList<ChurnFeatureRow> rows =
            await _store.GetActiveChurnFeatureRowsAsync(gymId, DateTime.UtcNow, cancellationToken);

        int scored = 0;

        foreach (ChurnFeatureRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ChurnPredictionOutput prediction = engine.Predict(new ChurnData
            {
                VisitsPerWeek = row.VisitsPerWeek,
                RecentVisits = row.RecentVisits,
                AttendanceDecline = row.AttendanceDecline,
                DaysSinceLastVisit = row.DaysSinceLastVisit,
                MembershipLengthMonths = row.MembershipLengthMonths,
                MembershipTypeMonths = row.MembershipTypeMonths,
                PaymentHistory = row.PaymentHistory,
                PauseCount = row.PauseCount
            });

            double probability = Math.Clamp(prediction.Probability, 0f, 1f);

            await _store.AddChurnPredictionAsync(new ChurnPrediction
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                MemberId = row.MemberId,
                ScoredAtUtc = DateTime.UtcNow,
                Probability = probability,
                RiskLevel = GetRiskLevel(probability),
                ModelVersion = "Current",
                FeatureImportanceJson = "{}"
            }, cancellationToken);

            scored++;
        }

        await _store.SaveChangesAsync(cancellationToken);
        return scored;
    }

    private static string GetRiskLevel(double probability) =>
        probability >= 0.66 ? "High" :
        probability >= 0.33 ? "Medium" :
        "Low";
}
