using Application.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor")]
[Route("api/reports")]
public sealed class ReportsController(IReportingService reportingService) : ControllerBase
{
    [HttpGet("session/{sessionId}")]
    public async Task<IActionResult> GetSessionReport(string sessionId, CancellationToken cancellationToken)
    {
        var report = await reportingService.BuildSessionReportAsync(sessionId, cancellationToken);
        return Ok(report);
    }
}
