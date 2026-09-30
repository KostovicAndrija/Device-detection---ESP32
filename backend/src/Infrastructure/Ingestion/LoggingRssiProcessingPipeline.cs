using Application.Ingestion;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Monitoring;
using Application.Risk;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Ingestion;

public sealed class LoggingRssiProcessingPipeline(
    ILogger<LoggingRssiProcessingPipeline> logger,
    IDeviceRepository deviceRepository,
    IObservationRepository observationRepository,
    IWhitelistRepository whitelistRepository,
    IAlertRepository alertRepository,
    IAppUnitOfWork unitOfWork,
    IDeviceHashingService hashingService,
    IRiskScoringService riskScoringService,
    IMonitoringEventPublisher monitoringEventPublisher,
    AppDbContext db) : IRssiProcessingPipeline
{
    public async Task ProcessAsync(RssiIngressMessage message, CancellationToken cancellationToken = default)
    {
        var registration = false;
        if (Guid.TryParse(message.SessionId, out var parsedSessionId))
        {
            var session = await db.ExamSessions.SingleOrDefaultAsync(s => s.Id == parsedSessionId, cancellationToken);
            if (session is null || session.Status != "active" || message.CreatedAt < session.StartsAt) return;
            if (session?.RegistrationExpiresAt is { } expires)
            {
                if (DateTimeOffset.UtcNow > expires || message.CreatedAt > expires) return;
                registration = true;
            }
        }
        if (!string.IsNullOrWhiteSpace(message.EventId) &&
            await observationRepository.ExistsByExternalIdAsync(message.EventId, cancellationToken))
        {
            logger.LogDebug("Ignoring duplicate ingestion event {EventId}", SanitizeForLog(message.EventId));
            return;
        }

        var deviceHash = hashingService.Hash(message.DeviceIdentifier);
        var sensorId = SanitizeForLog(message.SensorId);
        var sessionId = SanitizeForLog(message.SessionId ?? "n/a");

        var device = await deviceRepository.GetByHashAsync(deviceHash, cancellationToken);
        var unknownDevice = device is null;
        if (device is null)
        {
            device = Device.Create(deviceHash, message.CreatedAt, message.SignalType);
            await deviceRepository.AddAsync(device, cancellationToken);
        }
        else
        {
            device.Touch(message.CreatedAt);
        }

        var observation = DeviceObservation.Create(
            deviceId: device.Id,
            sensorId: message.SensorId,
            sessionId: message.SessionId,
            signalType: message.SignalType,
            rssi: message.Rssi,
            capturedAt: message.CreatedAt,
            externalId: message.EventId);

        await observationRepository.AddAsync(observation, cancellationToken);

        var isWhitelisted = !string.IsNullOrWhiteSpace(message.SessionId) &&
                            await whitelistRepository.IsWhitelistedAsync(message.SessionId, deviceHash, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var staffDevice = await db.WhitelistEntries.AnyAsync(w => w.SessionId == message.SessionId && w.DeviceHash == deviceHash &&
            w.StaffDeviceId != null && w.ValidFrom <= now && (w.ValidTo == null || w.ValidTo >= now), cancellationToken);
        var riskScore = registration || staffDevice ? 0 : riskScoringService.Calculate(message.Rssi, isWhitelisted, unknownDevice, message.SignalType);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await monitoringEventPublisher.PublishDeviceUpdatedAsync(
            new DeviceUpdatedEvent(deviceHash, message.SensorId, message.SessionId, message.Rssi, message.CreatedAt, riskScore),
            cancellationToken);

        if (riskScore >= 70 &&
            !await alertRepository.ExistsRecentAsync(device.Id, message.SessionId, TimeSpan.FromMinutes(2), cancellationToken))
        {
            var alert = Alert.Create(device.Id, message.SessionId, riskScore, "Risk threshold exceeded", DateTimeOffset.UtcNow);
            await alertRepository.AddAsync(alert, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await monitoringEventPublisher.PublishAlertCreatedAsync(
                new AlertCreatedEvent(alert.Id, deviceHash, message.SessionId, riskScore, alert.Reason, alert.CreatedAt),
                cancellationToken);
        }

        logger.LogInformation(
            "Processed RSSI reading for {DeviceHash} from sensor {SensorId} ({Rssi}) in session {SessionId}",
            deviceHash,
            sensorId,
            message.Rssi,
            sessionId);
    }

    private static string SanitizeForLog(string value)
        => value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
