using Application.Ingestion;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Ingestion;

public sealed class LoggingRssiProcessingPipeline(ILogger<LoggingRssiProcessingPipeline> logger) : IRssiProcessingPipeline
{
    public Task ProcessAsync(RssiIngressMessage message, CancellationToken cancellationToken = default)
    {
        var deviceHash = SanitizeForLog(message.DeviceHash);
        var sensorId = SanitizeForLog(message.SensorId);
        var sessionId = SanitizeForLog(message.SessionId ?? "n/a");

        logger.LogInformation(
            "Processed RSSI reading for {DeviceHash} from sensor {SensorId} ({Rssi}) in session {SessionId}",
            deviceHash,
            sensorId,
            message.Rssi,
            sessionId);

        return Task.CompletedTask;
    }

    private static string SanitizeForLog(string value)
        => value
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
