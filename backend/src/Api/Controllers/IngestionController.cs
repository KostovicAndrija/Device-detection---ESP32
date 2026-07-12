using Application.Ingestion;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/ingestion")]
public sealed class IngestionController(IRssiIngestionService ingestionService) : ControllerBase
{
    [HttpPost("rssi")]
    public async Task<IActionResult> IngestRssi([FromBody] RssiIngressMessage request, CancellationToken cancellationToken)
    {
        await ingestionService.IngestAsync(request, cancellationToken);
        return Accepted();
    }
}
