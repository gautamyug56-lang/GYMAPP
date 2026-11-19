namespace Rackroom.Application.ML;

public interface IChurnModelStore
{
    string ModelPath { get; }
    string ReportPath { get; }
    Task SaveModelAsync(Microsoft.ML.ITransformer model, Microsoft.ML.DataViewSchema schema, CancellationToken cancellationToken = default);
    Task<(Microsoft.ML.ITransformer Model, Microsoft.ML.DataViewSchema Schema)> LoadModelAsync(CancellationToken cancellationToken = default);
    Task SaveReportAsync(ChurnTrainingResult result, CancellationToken cancellationToken = default);
    Task<ChurnTrainingResult?> LoadReportAsync(CancellationToken cancellationToken = default);
}
