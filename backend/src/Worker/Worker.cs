using Application.Ingestion;
using MQTTnet;
using MQTTnet.Client;

namespace Worker;

public sealed class MqttWorker(
    ILogger<MqttWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration) : BackgroundService
{
    private readonly string _host = configuration.GetValue<string>("Mqtt:Host") ?? "localhost";
    private readonly int _port = configuration.GetValue<int?>("Mqtt:Port") ?? 1883;
    private readonly string _topic = configuration.GetValue<string>("Mqtt:Topic") ?? "sensors/+/rssi";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        using var mqttClient = factory.CreateMqttClient();

        mqttClient.ApplicationMessageReceivedAsync += async messageEvent =>
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var decoder = scope.ServiceProvider.GetRequiredService<ISensorMessageDecoder>();
                var payload = messageEvent.ApplicationMessage.PayloadSegment.Array is null
                    ? null : decoder.Decode(messageEvent.ApplicationMessage.PayloadSegment.AsMemory());

                if (payload is null)
                {
                    return;
                }

                var ingestionService = scope.ServiceProvider.GetRequiredService<IRssiIngestionService>();

                var ingressMessage = payload;

                await ingestionService.IngestAsync(ingressMessage, stoppingToken);
                logger.LogInformation("Processed MQTT message from {SensorId} with RSSI {Rssi}", payload.SensorId, payload.Rssi);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process MQTT payload");
            }
        };

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(_host, _port)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!mqttClient.IsConnected)
                {
                    await mqttClient.ConnectAsync(options, stoppingToken);
                    await mqttClient.SubscribeAsync(_topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);
                    logger.LogInformation("Connected to MQTT broker {Host}:{Port} and subscribed to {Topic}", _host, _port, _topic);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MQTT connect/subscribe failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

}
