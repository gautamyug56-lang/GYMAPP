using Microsoft.ML.Data;

namespace Rackroom.Application.ML;

public sealed class ChurnData
{
    [LoadColumn(0)]
    public float VisitsPerWeek { get; set; }

    [LoadColumn(1)]
    public float RecentVisits { get; set; }

    [LoadColumn(2)]
    public float AttendanceDecline { get; set; }

    [LoadColumn(3)]
    public float DaysSinceLastVisit { get; set; }

    [LoadColumn(4)]
    public float MembershipLengthMonths { get; set; }

    [LoadColumn(5)]
    public float MembershipTypeMonths { get; set; }

    [LoadColumn(6)]
    public float PaymentHistory { get; set; }

    [LoadColumn(7)]
    public float PauseCount { get; set; }

    [LoadColumn(8)]
    public bool Label { get; set; }
}

public sealed class ChurnPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}

public sealed record ChurnFeatureRow(
    Guid MemberId,
    float VisitsPerWeek,
    float RecentVisits,
    float AttendanceDecline,
    float DaysSinceLastVisit,
    float MembershipLengthMonths,
    float MembershipTypeMonths,
    float PaymentHistory,
    float PauseCount);

public sealed record ChurnTrainingMetrics(
    string Trainer,
    double Precision,
    double Recall,
    double F1,
    double Auc,
    long TruePositive,
    long FalsePositive,
    long TrueNegative,
    long FalseNegative);

public sealed record ChurnFeatureImportance(
    string Feature,
    double Importance);

public sealed record ChurnTrainingResult(
    string ChosenTrainer,
    string ModelVersion,
    IReadOnlyList<ChurnTrainingMetrics> Metrics,
    IReadOnlyList<ChurnFeatureImportance> FeatureImportance,
    int TrainingRows,
    int TestRows,
    string DatasetDescription);
