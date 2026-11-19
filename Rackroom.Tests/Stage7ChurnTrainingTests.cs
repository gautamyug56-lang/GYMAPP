using Microsoft.ML;
using Rackroom.Application.ML;

namespace Rackroom.Tests;

public class Stage7ChurnTrainingTests
{
    [Fact]
    public async Task Churn_training_compares_three_trainers_and_selects_a_model()
    {
        var store = new InMemoryChurnModelStore();
        var trainer = new ChurnModelTrainer(store);

        ChurnTrainingResult result = await trainer.TrainAsync();

        Assert.Equal(3, result.Metrics.Count);
        Assert.Contains(result.Metrics, x => x.Trainer == "SDCA Logistic Regression");
        Assert.Contains(result.Metrics, x => x.Trainer == "FastTree");
        Assert.Contains(result.Metrics, x => x.Trainer == "LightGBM");
        Assert.NotEmpty(result.ChosenTrainer);
        Assert.NotEmpty(result.FeatureImportance);
        Assert.Equal(2400, result.TrainingRows);
        Assert.Equal(600, result.TestRows);

        foreach (ChurnTrainingMetrics metrics in result.Metrics)
        {
            Console.WriteLine(
                $"{metrics.Trainer}: Precision={metrics.Precision:F3}, Recall={metrics.Recall:F3}, F1={metrics.F1:F3}, AUC={metrics.Auc:F3}, TP={metrics.TruePositive}, FP={metrics.FalsePositive}, TN={metrics.TrueNegative}, FN={metrics.FalseNegative}");
        }

        Console.WriteLine($"Selected model: {result.ChosenTrainer}");
        Console.WriteLine("Top PFI features:");
        foreach (ChurnFeatureImportance feature in result.FeatureImportance.Take(5))
            Console.WriteLine($"  {feature.Feature}: {feature.Importance:F4}");
    }

    private sealed class InMemoryChurnModelStore : IChurnModelStore
    {
        public string ModelPath => "memory";
        public string ReportPath => "memory";

        public Task SaveModelAsync(
            ITransformer model,
            DataViewSchema schema,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<(ITransformer Model, DataViewSchema Schema)> LoadModelAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveReportAsync(
            ChurnTrainingResult result,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<ChurnTrainingResult?> LoadReportAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ChurnTrainingResult?>(null);
    }
}
