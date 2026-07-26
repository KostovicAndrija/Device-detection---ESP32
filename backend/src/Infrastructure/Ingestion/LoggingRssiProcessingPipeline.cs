using Application.Ingestion;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Monitoring;
using Application.Risk;
using Domain.Entities;
using Microsoft.Extensions.Logging;

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
    IMonitoringEventPublisher monitoringEventPublisher) : IRssiProcessingPipeline
{
    public async Task ProcessAsync(RssiIngressMessage message, CancellationToken cancellationToken = default)
    {
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
            capturedAt: message.CreatedAt);

        await observationRepository.AddAsync(observation, cancellationToken);

        var isWhitelisted = !string.IsNullOrWhiteSpace(message.SessionId) &&
                            await whitelistRepository.IsWhitelistedAsync(message.SessionId, deviceHash, cancellationToken);

        var riskScore = riskScoringService.Calculate(message.Rssi, isWhitelisted, unknownDevice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await monitoringEventPublisher.PublishDeviceUpdatedAsync(
            new DeviceUpdatedEvent(deviceHash, message.SensorId, message.SessionId, message.Rssi, message.CreatedAt, riskScore),
            cancellationToken);

        if (riskScore >= 70)
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
