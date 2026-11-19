using Rackroom.Application.Abstractions;
using Rackroom.Application.ML;

namespace Rackroom.Web.Background;

public sealed class ChurnScoringBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChurnScoringBackgroundService> _logger;
    public ChurnScoringBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ChurnScoringBackgroundService> logger) { _scopeFactory = scopeFactory; _logger = logger; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ScoreAllGymsAsync(stoppingToken);
        using PeriodicTimer timer = new(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await ScoreAllGymsAsync(stoppingToken);
    }
    private async Task ScoreAllGymsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IChurnScoringService scorer = scope.ServiceProvider.GetRequiredService<IChurnScoringService>();
            IRackroomDataStore store = scope.ServiceProvider.GetRequiredService<IRackroomDataStore>();
            foreach (Guid gymId in await store.GetGymIdsAsync(cancellationToken))
            {
                try { int count = await scorer.ScoreGymAsync(gymId, cancellationToken); _logger.LogInformation("Nightly churn scoring completed for gym {GymId}: {Count} members scored.", gymId, count); }
                catch (FileNotFoundException) { _logger.LogWarning("Skipping churn scoring for gym {GymId}: no trained model exists yet.", gymId); }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) { _logger.LogError(exception, "Nightly churn scoring job failed."); }
    }
}