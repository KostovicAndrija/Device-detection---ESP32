using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Monitoring;

public sealed class ObservationRetentionWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ObservationRetentionWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var retentionHours = Math.Max(1, configuration.GetValue("Retention:ObservationHours", 24));
                var cutoff = DateTimeOffset.UtcNow.AddHours(-retentionHours);
                var expiredIds = await dbContext.ExamSessions
                    .Where(x => x.Status == "completed" && x.EndsAt < cutoff)
                    .Select(x => x.Id)
                    .ToListAsync(stoppingToken);
                var sessionIds = expiredIds.Select(x => x.ToString()).ToList();
                if (sessionIds.Count > 0)
                {
                    var deleted = await dbContext.DeviceObservations
                        .Where(x => x.SessionId != null && sessionIds.Contains(x.SessionId))
                        .ExecuteDeleteAsync(stoppingToken);
                    if (deleted > 0)
                    {
                        logger.LogInformation("Retention removed {Count} expired observations.", deleted);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Observation retention failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
