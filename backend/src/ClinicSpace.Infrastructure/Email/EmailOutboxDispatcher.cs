using ClinicSpace.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSpace.Infrastructure.Email;

/// <summary>
/// Background worker that sends queued emails from the outbox table, with exponential back-off
/// on failure. Emails that keep failing are dead-lettered and logged for follow-up.
/// Designed for a single API instance; see docs/ARCHITECTURE.md for how to scale it out.
/// </summary>
public sealed class EmailOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    TimeProvider clock,
    ILogger<EmailOutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.DispatcherEnabled)
        {
            logger.LogInformation("Email outbox dispatcher is disabled");
            return;
        }

        logger.LogInformation("Email outbox dispatcher started in {Mode} mode", settings.Mode);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, settings.PollIntervalSeconds)));
        do
        {
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // A database outage must not kill the worker; try again on the next tick.
                logger.LogError(ex, "Email outbox dispatch cycle failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>Sends one batch of due emails. Public so tests can run a cycle directly.</summary>
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var transport = scope.ServiceProvider.GetRequiredService<IEmailTransport>();
        var now = clock.GetUtcNow();

        var due = await db.EmailMessages
            .Where(m => m.SentAtUtc == null && !m.IsDeadLettered && m.NextAttemptAtUtc <= now)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var message in due)
        {
            try
            {
                await transport.SendAsync(message, cancellationToken);
                message.MarkSent(clock.GetUtcNow());
                sent++;
                logger.LogInformation("Email {EmailId} sent for request {RequestId}", message.Id, message.RelatedRequestId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.MarkFailed(ex.Message, clock.GetUtcNow(), settings.MaxAttempts);
                if (message.IsDeadLettered)
                {
                    logger.LogError(ex, "Email {EmailId} dead-lettered after {Attempts} attempts", message.Id, message.Attempts);
                }
                else
                {
                    logger.LogWarning(ex, "Email {EmailId} failed (attempt {Attempts}); retrying at {NextAttempt}", message.Id, message.Attempts, message.NextAttemptAtUtc);
                }
            }

            // Save after each email so a crash cannot cause a sent email to be sent again.
            await db.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }
}
