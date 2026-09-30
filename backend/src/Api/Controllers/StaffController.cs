using Application.Abstractions.Security;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = "Professor,Assistant")]
[Route("api/staff")]
public sealed class StaffController(AppDbContext db, ICurrentUser current, IPasswordHasher<AppUser> passwords,
    Application.Localization.IRoomLayoutProvider layouts) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await db.Users.Where(u => u.Id == current.Id)
        .Select(u => new { u.Id, u.Username, u.Role }).SingleAsync(ct));

    [Authorize(Roles = "Professor")]
    [HttpPost("assistants")]
    public async Task<IActionResult> CreateAssistant(CreateAssistantRequest request, CancellationToken ct)
    {
        var name = request.Username.Trim();
        if (name.Length is < 3 or > 80 || request.Password.Length < 12)
            throw new ArgumentException("Korisničko ime mora imati 3 do 80 znakova, a lozinka najmanje 12.");
        if (await db.Users.AnyAsync(u => u.Username == name, ct)) throw new InvalidOperationException("Korisničko ime je zauzeto.");
        var user = AppUser.Create(name, "Assistant");
        user.SetPasswordHash(passwords.HashPassword(user, request.Password));
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return Ok(new { user.Id, user.Username, user.Role });
    }

    [HttpGet("devices")]
    public async Task<IActionResult> Devices(CancellationToken ct) => Ok(await db.StaffDevices
        .Where(d => d.UserId == current.Id).OrderBy(d => d.CreatedAt).ToListAsync(ct));

    [HttpGet("scans/current")]
    public async Task<IActionResult> CurrentScan(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);
        return Ok(await db.ExamSessions.Where(s => s.OwnerUserId == current.Id && s.Status == "active" && s.RegistrationExpiresAt > cutoff)
            .OrderByDescending(s => s.StartsAt).FirstOrDefaultAsync(ct));
    }

    [HttpPost("scans")]
    public async Task<IActionResult> StartScan(StartScanRequest request, CancellationToken ct)
    {
        var user = await EditableUser(ct);
        var now = DateTimeOffset.UtcNow;
        if (await db.ExamSessions.AnyAsync(s => s.Status == "active" &&
            (s.RegistrationExpiresAt == null || s.RegistrationExpiresAt > now) &&
            (s.RoomId == request.RoomId.Trim() || s.OwnerUserId == user.Id), ct))
            throw new InvalidOperationException("Prvo zaustavite postojeće skeniranje ili izaberite slobodnu učionicu.");
        var scan = ExamSession.Create("Registracija ličnih uređaja", request.RoomId, now);
        scan.SetOwner(user.Id);
        scan.MarkAsRegistrationScan(now.AddMinutes(2));
        await layouts.CaptureAsync(scan, ct);
        scan.Start(now);
        db.ExamSessions.Add(scan);
        await db.SaveChangesAsync(ct);
        return Ok(scan);
    }

    [HttpGet("scans/{id:guid}")]
    public async Task<IActionResult> Scan(Guid id, CancellationToken ct)
    {
        var scan = await OwnScan(id, ct);
        var observations = await db.DeviceObservations.Where(o => o.SessionId == id.ToString() &&
            o.CapturedAt >= scan.StartsAt && o.CapturedAt <= scan.RegistrationExpiresAt).ToListAsync(ct);
        var ids = observations.Select(o => o.DeviceId).Distinct().ToList();
        var devices = await db.Devices.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        return Ok(new { scan, candidates = devices.Select(d => new {
            d.Id, d.HashId, d.Type, rssi = observations.Where(o => o.DeviceId == d.Id).Max(o => o.Rssi)
        }) });
    }

    [HttpPost("scans/{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, ConfirmScanRequest request, CancellationToken ct)
    {
        var user = await EditableUser(ct);
        var scan = await OwnScan(id, ct);
        if (scan.Status != "active" || DateTimeOffset.UtcNow > scan.RegistrationExpiresAt!.Value.AddMinutes(10))
            throw new InvalidOperationException("Pokrenite novo registraciono skeniranje.");
        if (request.Devices.Count is < 1 or > 20 || request.Devices.Select(d => d.DeviceId).Distinct().Count() != request.Devices.Count)
            throw new ArgumentException("Izaberite između 1 i 20 svojih uređaja.");
        foreach (var selected in request.Devices)
        {
            if (!await db.DeviceObservations.AnyAsync(o => o.SessionId == id.ToString() && o.DeviceId == selected.DeviceId &&
                o.CapturedAt >= scan.StartsAt && o.CapturedAt <= scan.RegistrationExpiresAt, ct))
                throw new ArgumentException("Uređaj nije pronađen tokom ovog skeniranja.");
            var device = await db.Devices.SingleAsync(d => d.Id == selected.DeviceId, ct);
            var existing = await db.StaffDevices.SingleOrDefaultAsync(d => d.DeviceHash == device.HashId, ct);
            if (existing is not null && existing.UserId != user.Id)
                throw new InvalidOperationException("Uređaj je već registrovan na drugom nalogu.");
            if (existing is null) db.StaffDevices.Add(StaffDevice.Create(user.Id, selected.Label, device.HashId, id));
        }
        scan.Stop(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("scans/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var scan = await OwnScan(id, ct);
        if (scan.Status == "active") scan.Stop(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("devices/{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
    {
        await EditableUser(ct);
        var device = await db.StaffDevices.SingleOrDefaultAsync(d => d.Id == id && d.UserId == current.Id, ct);
        if (device is null) return NotFound();
        db.StaffDevices.Remove(device);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<AppUser> EditableUser(CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(u => u.Id == current.Id, ct);
        if (await db.ExamSessions.AnyAsync(s => s.OwnerUserId == user.Id && s.Status == "active" && s.RegistrationExpiresAt == null, ct))
            throw new InvalidOperationException("Zaustavite praćenje pre promene ličnih uređaja.");
        user.TouchStaffDevices();
        return user;
    }

    private async Task<ExamSession> OwnScan(Guid id, CancellationToken ct) =>
        await db.ExamSessions.SingleOrDefaultAsync(s => s.Id == id && s.OwnerUserId == current.Id && s.RegistrationExpiresAt != null, ct)
        ?? throw new UnauthorizedAccessException("Nemate pristup ovom registracionom skeniranju.");

    public sealed record CreateAssistantRequest(string Username, string Password);
    public sealed record StartScanRequest(string RoomId);
    public sealed record SelectedDevice(Guid DeviceId, string Label);
    public sealed record ConfirmScanRequest(List<SelectedDevice> Devices);
}
