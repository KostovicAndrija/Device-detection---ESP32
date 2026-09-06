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
            request.CapturedAt ?? DateTimeOffset.UtcNow,
            request.EventId);

        await ingestionService.IngestAsync(message, cancellationToken);
        return Accepted();
    }

    [HttpPost("batch")]
    public async Task<IActionResult> IngestBatch([FromBody] IReadOnlyList<IngestObservationRequest> batch, CancellationToken cancellationToken)
    {
        if (batch.Count is 0 or > 500)
        {
            return BadRequest(new ProblemDetails { Title = "Batch must contain between 1 and 500 observations." });
        }

        foreach (var request in batch)
        {
            var message = new RssiIngressMessage(
                request.DeviceIdentifier,
                request.SensorId,
                request.SessionId,
                request.SignalType,
                request.Rssi,
                request.CapturedAt ?? DateTimeOffset.UtcNow,
                request.EventId);

            await ingestionService.IngestAsync(message, cancellationToken);
        }

        return Accepted(new { processed = batch.Count });
    }

    public sealed record IngestObservationRequest(
        [Required] string DeviceIdentifier,
        [Required] string SensorId,
        string? SessionId,
        [Required, RegularExpression("^(wifi|ble|bluetooth|bluetooth_pairing)$")] string SignalType,
        [Range(-120, 0)] double Rssi,
        DateTimeOffset? CapturedAt,
        [StringLength(128)] string? EventId);
}
