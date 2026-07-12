using Application.Ingestion;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Ingestion;

public sealed class LoggingRssiProcessingPipeline(ILogger<LoggingRssiProcessingPipeline> logger) : IRssiProcessingPipeline
{
    public Task ProcessAsync(RssiIngressMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Processed RSSI reading for {DeviceHash} from sensor {SensorId} ({Rssi}) in session {SessionId}",
            message.DeviceHash,
            message.SensorId,
            message.Rssi,
            message.SessionId ?? "n/a");

        return Task.CompletedTask;
    }
}
