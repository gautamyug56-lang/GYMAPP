using Microsoft.Extensions.Options;
using Rackroom.Application.Abstractions;
using Rackroom.Infrastructure.Email;

namespace Rackroom.Web.Background;

public sealed class ExpiryReminderBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromDays(7);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<SmtpOptions> _smtpOptions;
    private readonly ILogger<ExpiryReminderBackgroundService> _logger;

    public ExpiryReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<ExpiryReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _smtpOptions = smtpOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once when the application starts, then periodically.
        await ProcessRemindersAsync(stoppingToken);

        using PeriodicTimer timer = new(CheckInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessRemindersAsync(stoppingToken);
        }
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        if (!_smtpOptions.Value.Enabled)
        {
            _logger.LogDebug("Expiry reminder email job is disabled.");
            return;
        }

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();

            IRackroomDataStore dataStore =
                scope.ServiceProvider.GetRequiredService<IRackroomDataStore>();

            IEmailSender emailSender =
                scope.ServiceProvider.GetRequiredService<IEmailSender>();

            IReadOnlyList<Guid> gymIds =
                await dataStore.GetGymIdsAsync(cancellationToken);

            DateTime nowUtc = DateTime.UtcNow;
            DateTime reminderWindowEndUtc = nowUtc.Add(ReminderWindow);

            foreach (Guid gymId in gymIds)
            {
                IReadOnlyList<Rackroom.Domain.Entities.Membership> memberships =
                    await dataStore.GetMembershipsForExpiryReminderAsync(
                        gymId,
                        nowUtc,
                        reminderWindowEndUtc,
                        cancellationToken);

                foreach (var membership in memberships)
                {
                    if (string.IsNullOrWhiteSpace(membership.Member?.Email))
                    {
                        _logger.LogWarning(
                            "Skipping expiry reminder for membership {MembershipId}: member has no email.",
                            membership.Id);
                        continue;
                    }

                    try
                    {
                        string memberName =
                            $"{membership.Member.FirstName} {membership.Member.LastName}".Trim();

                        string subject = "Rackroom membership expiry reminder";
                        string body =
                            $"Hello {memberName},

Your Rackroom membership is due to expire on {membership.EndDateUtc:yyyy-MM-dd}.

Membership plan: {membership.MembershipPlan?.Name ?? "Membership"}

Please contact the gym if you would like to renew your membership.

Regards,
Rackroom";

                        await emailSender.SendAsync(
                            membership.Member.Email,
                            subject,
                            body,
                            cancellationToken);

                        membership.ExpiryReminderSentAtUtc = DateTime.UtcNow;
                        await dataStore.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Sent expiry reminder for membership {MembershipId}.",
                            membership.Id);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(
                            exception,
                            "Failed to send expiry reminder for membership {MembershipId}.",
                            membership.Id);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Expiry reminder background job failed.");
        }
    }
}
