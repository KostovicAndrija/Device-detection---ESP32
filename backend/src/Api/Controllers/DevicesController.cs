using Application.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Application.Abstractions.Security;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor,Assistant")]
[Route("api/devices")]
public sealed class DevicesController(IDeviceQueryService queryService, AppDbContext db, ICurrentUser current) : ControllerBase
{
    [HttpGet("active")]
    public async Task<IActionResult> GetActive([FromQuery] int windowMinutes = 10, CancellationToken cancellationToken = default)
    {
        if (current.IsAssistant)
        {
            var since = DateTimeOffset.UtcNow.AddMinutes(-Math.Clamp(windowMinutes, 1, 1440));
            var sessions = await db.ExamSessions.Where(s => s.OwnerUserId == current.Id && s.RegistrationExpiresAt == null).Select(s => s.Id.ToString()).ToListAsync(cancellationToken);
            var observations = await db.DeviceObservations.Where(o => sessions.Contains(o.SessionId!) && o.CapturedAt >= since).ToListAsync(cancellationToken);
            var ids = observations.Select(o => o.DeviceId).Distinct().ToList();
            var ownDevices = await db.Devices.Where(d => ids.Contains(d.Id)).ToListAsync(cancellationToken);
            return Ok(ownDevices.Select(d => new DeviceDto(d.Id, d.HashId, d.Type,
                observations.Where(o => o.DeviceId == d.Id).Min(o => o.CapturedAt), observations.Where(o => o.DeviceId == d.Id).Max(o => o.CapturedAt))));
        }
        var devices = await queryService.GetActiveAsync(TimeSpan.FromMinutes(Math.Max(1, windowMinutes)), cancellationToken);
        return Ok(devices);
    }
}
