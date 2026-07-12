using Application.Ingestion;

namespace Worker;

public sealed class MqttWorker(ILogger<MqttWorker> logger, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IRssiIngestionService>();

            var sampleMessage = new RssiIngressMessage(
                DeviceHash: "sample-device-hash",
                SensorId: "sensor-01",
                SessionId: null,
                Rssi: -58,
                CreatedAt: DateTimeOffset.UtcNow);

            await ingestionService.IngestAsync(sampleMessage, stoppingToken);
            logger.LogInformation("MQTT ingestion worker pushed sample RSSI reading at {Timestamp}", sampleMessage.CreatedAt);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
