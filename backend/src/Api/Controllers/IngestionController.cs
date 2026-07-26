using Application.Ingestion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor")]
[Route("api/ingestion")]
public sealed class IngestionController(IRssiIngestionService ingestionService) : ControllerBase
{
    [HttpPost("observation")]
    public async Task<IActionResult> IngestObservation([FromBody] IngestObservationRequest request, CancellationToken cancellationToken)
    {
        var message = new RssiIngressMessage(
            request.DeviceIdentifier,
            request.SensorId,
            request.SessionId,
            request.SignalType,
            request.Rssi,
            request.CapturedAt ?? DateTimeOffset.UtcNow);

        await ingestionService.IngestAsync(message, cancellationToken);
        return Accepted();
    }

    [HttpPost("batch")]
    public async Task<IActionResult> IngestBatch([FromBody] IReadOnlyList<IngestObservationRequest> batch, CancellationToken cancellationToken)
    {
        foreach (var request in batch)
        {
            var message = new RssiIngressMessage(
                request.DeviceIdentifier,
                request.SensorId,
                request.SessionId,
                request.SignalType,
                request.Rssi,
                request.CapturedAt ?? DateTimeOffset.UtcNow);

            await ingestionService.IngestAsync(message, cancellationToken);
        }

        return Accepted(new { processed = batch.Count });
    }

    public sealed record IngestObservationRequest(
        [Required] string DeviceIdentifier,
        [Required] string SensorId,
        string? SessionId,
        [Required] string SignalType,
        [Range(-120, 0)] double Rssi,
        DateTimeOffset? CapturedAt);
}
