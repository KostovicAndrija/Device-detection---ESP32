using Application.Ingestion;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                var topic = messageEvent.ApplicationMessage.Topic;
                if (topic.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
                {
                    var json = messageEvent.ApplicationMessage.PayloadSegment.AsMemory();
                    if (json.IsEmpty) return;
                    var status = JsonSerializer.Deserialize<SensorStatusPayload>(json.Span,
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                    if (status is null || string.IsNullOrWhiteSpace(status.SensorId) || string.IsNullOrWhiteSpace(status.ClassroomId)) return;
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var node = await db.SensorNodes.SingleOrDefaultAsync(x => x.Id == status.SensorId && x.ClassroomId == status.ClassroomId, stoppingToken);
                    var timestamp = status.Timestamp ?? DateTimeOffset.UtcNow;
                    if (timestamp > DateTimeOffset.UtcNow.AddMinutes(1)) return;
                    if (node is null) db.SensorNodes.Add(SensorNode.Register(status.SensorId, status.ClassroomId, timestamp, status.FirmwareVersion, status.IsSimulated));
                    else node.Heartbeat(timestamp, status.FirmwareVersion, status.IsSimulated);
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogDebug("Heartbeat received from sensor {SensorId} in {ClassroomId}", status.SensorId, status.ClassroomId);
                    return;
                }
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
                    await mqttClient.SubscribeAsync("sensors/+/status", MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, stoppingToken);
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

    private sealed record SensorStatusPayload(string SensorId, string ClassroomId, DateTimeOffset? Timestamp,
        string? FirmwareVersion, bool IsSimulated = false);

}
