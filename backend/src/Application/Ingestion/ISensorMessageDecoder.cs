using System.Text.Json;
namespace Application.Ingestion;

// Hardware-specific payloads are translated here, before entering the common pipeline.
public interface ISensorMessageDecoder
{
    RssiIngressMessage? Decode(ReadOnlyMemory<byte> payload);
}
public sealed class JsonSensorMessageDecoder : ISensorMessageDecoder
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    public RssiIngressMessage? Decode(ReadOnlyMemory<byte> payload)
    {
        var value = JsonSerializer.Deserialize<Payload>(payload.Span, Options);
        return value is null ? null : new(value.DeviceIdentifier, value.SensorId, value.SessionId, value.SignalType,
            value.Rssi, value.Timestamp ?? DateTimeOffset.UtcNow, value.EventId);
    }
    private sealed record Payload(string DeviceIdentifier, string SensorId, string? SessionId, string SignalType,
        double Rssi, DateTimeOffset? Timestamp, string? EventId);
}
