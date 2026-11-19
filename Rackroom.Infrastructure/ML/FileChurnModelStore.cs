using System.Text.Json;
using Microsoft.ML;
using Rackroom.Application.ML;

namespace Rackroom.Infrastructure.ML;

public sealed class FileChurnModelStore : IChurnModelStore
{
    private readonly string _root;

    public FileChurnModelStore(string contentRootPath)
    {
        _root = Path.Combine(contentRootPath, "App_Data", "ml");
        Directory.CreateDirectory(_root);
    }

    public string ModelPath => Path.Combine(_root, "churn-model.zip");
    public string ReportPath => Path.Combine(_root, "churn-training-report.json");

    public async Task SaveModelAsync(
        ITransformer model,
        DataViewSchema schema,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var mlContext = new MLContext();
        mlContext.Model.Save(model, schema, ModelPath);
        await Task.CompletedTask;
    }

    public async Task<(ITransformer Model, DataViewSchema Schema)> LoadModelAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(ModelPath))
            throw new FileNotFoundException("The churn model has not been trained yet.", ModelPath);

        var mlContext = new MLContext();
        using FileStream stream = File.OpenRead(ModelPath);
        ITransformer model = mlContext.Model.Load(stream, out DataViewSchema schema);
        await Task.CompletedTask;
        return (model, schema);
    }

    public async Task<ChurnTrainingResult?> LoadReportAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ReportPath))
            return null;

        string json = await File.ReadAllTextAsync(ReportPath, cancellationToken);
        return JsonSerializer.Deserialize<ChurnTrainingResult>(json);
    }

    public async Task SaveReportAsync(
        ChurnTrainingResult result,
        CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ReportPath, json, cancellationToken);
    }
}
