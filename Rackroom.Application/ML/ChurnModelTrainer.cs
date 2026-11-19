using Microsoft.ML;
using Microsoft.ML.Data;
using Rackroom.Application.Exceptions;

namespace Rackroom.Application.ML;

public interface IChurnModelTrainer
{
    Task<ChurnTrainingResult> TrainAsync(CancellationToken cancellationToken = default);
}

public sealed class ChurnModelTrainer : IChurnModelTrainer
{
    private static readonly string[] FeatureColumns =
    [
        nameof(ChurnData.VisitsPerWeek),
        nameof(ChurnData.RecentVisits),
        nameof(ChurnData.AttendanceDecline),
        nameof(ChurnData.DaysSinceLastVisit),
        nameof(ChurnData.MembershipLengthMonths),
        nameof(ChurnData.MembershipTypeMonths),
        nameof(ChurnData.PaymentHistory),
        nameof(ChurnData.PauseCount)
    ];

    private readonly IChurnModelStore _modelStore;

    public ChurnModelTrainer(IChurnModelStore modelStore)
    {
        _modelStore = modelStore;
    }

    public async Task<ChurnTrainingResult> TrainAsync(CancellationToken cancellationToken = default)
    {
        var generator = new SyntheticChurnDatasetGenerator();
        IReadOnlyList<ChurnData> rows = generator.Generate(3000);

        MLContext ml = new(seed: 20261119);
        IDataView data = ml.Data.LoadFromEnumerable(rows);

        DataOperationsCatalog.TrainTestData split = ml.Data.TrainTestSplit(
            data,
            testFraction: 0.20,
            seed: 20261119);

        IEstimator<ITransformer> prep = ml.Transforms.Concatenate(
                "Features",
                FeatureColumns)
            .Append(ml.Transforms.NormalizeMinMax("Features"))
            .AppendCacheCheckpoint(ml);

        var trainers = new (string Name, IEstimator<ITransformer> Estimator)[]
        {
            (
                "SDCA Logistic Regression",
                prep.Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
            ),
            (
                "FastTree",
                prep.Append(ml.BinaryClassification.Trainers.FastTree(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
            ),
            (
                "LightGBM",
                prep.Append(ml.BinaryClassification.Trainers.LightGbm(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
            )
        };

        var results = new List<ChurnTrainingMetrics>();
        ITransformer? bestModel = null;
        double bestF1 = double.MinValue;
        double bestAuc = double.MinValue;
        string bestTrainer = string.Empty;

        foreach (var trainer in trainers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ITransformer model = trainer.Estimator.Fit(split.TrainSet);
            IDataView predictions = model.Transform(split.TestSet);
            BinaryClassificationMetrics metrics =
                ml.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

            var confusion = metrics.ConfusionMatrix.Counts;

            ChurnTrainingMetrics result = new(
                trainer.Name,
                metrics.PositivePrecision,
                metrics.PositiveRecall,
                metrics.F1Score,
                metrics.AreaUnderRocCurve,
                (long)confusion[1][1],
                (long)confusion[0][1],
                (long)confusion[0][0],
                (long)confusion[1][0]);

            results.Add(result);

            if (result.F1 > bestF1 ||
                (Math.Abs(result.F1 - bestF1) < 0.000001 && result.Auc > bestAuc))
            {
                bestF1 = result.F1;
                bestAuc = result.Auc;
                bestTrainer = trainer.Name;
                bestModel = model;
            }
        }

        if (bestModel is null)
            throw new BusinessRuleException("No churn model was trained.");

        IDataView transformedTest = bestModel.Transform(split.TestSet);
        var importance = ml.BinaryClassification
            .PermutationFeatureImportanceNonCalibrated(
                bestModel,
                transformedTest,
                labelColumnName: "Label",
                permutationCount: 3);

        IReadOnlyList<ChurnFeatureImportance> featureImportance =
            importance
                .Select((metric, index) => new ChurnFeatureImportance(
                    FeatureColumns[index],
                    Math.Abs(metric.Value.AreaUnderRocCurve.Mean)))
                .OrderByDescending(x => x.Importance)
                .ToList();

        ITransformer finalModel = trainers
            .Single(x => x.Name == bestTrainer)
            .Estimator
            .Fit(data);

        string modelVersion = $"{bestTrainer.Replace(" ", "-").ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        await _modelStore.SaveModelAsync(finalModel, data.Schema, cancellationToken);

        ChurnTrainingResult trainingResult = new(
            bestTrainer,
            modelVersion,
            results,
            featureImportance,
            rows.Count - (int)Math.Round(rows.Count * 0.20),
            (int)Math.Round(rows.Count * 0.20),
            "Synthetic data generated deterministically from documented assumptions. No real member data was used.");

        await _modelStore.SaveReportAsync(trainingResult, cancellationToken);

        return trainingResult;
    }
}
