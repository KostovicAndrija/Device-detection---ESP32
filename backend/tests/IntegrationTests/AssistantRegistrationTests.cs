using Api.Controllers;
using Api.Monitoring;
using Application.Abstractions.Security;
using Application.Ingestion;
using Application.Risk;
using Application.Sessions;
using Domain.Entities;
using Infrastructure.Ingestion;
using Infrastructure.Monitoring;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IntegrationTests;

public sealed class AssistantRegistrationTests
{
    private sealed record Actor(Guid? Id, bool IsProfessor = false, bool IsAssistant = true) : ICurrentUser;
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static StaffController Controller(AppDbContext db, AppUser user) => new(db, new Actor(user.Id), new PasswordHasher<AppUser>());
    private static ExamSessionService Sessions(AppDbContext db, AppUser user) => new(new ExamSessionRepository(db), new AppUnitOfWork(db), new NoopMonitoringEventPublisher(), new SessionAccess(db, new Actor(user.Id)));
    private static LoggingRssiProcessingPipeline Pipeline(AppDbContext db) => new(NullLogger<LoggingRssiProcessingPipeline>.Instance,
        new DeviceRepository(db), new ObservationRepository(db), new WhitelistRepository(db), new AlertRepository(db), new AppUnitOfWork(db),
        new Sha256DeviceHashingService(Options.Create(new HashingOptions())), new RiskScoringService(), new NoopMonitoringEventPublisher(), db);

    [Fact]
    public async Task Registration_then_monitoring_whitelists_personal_device_but_alerts_for_unknown_device()
    {
        await using var db = Database();
        var user = AppUser.Create("assistant", "Assistant"); db.Users.Add(user); await db.SaveChangesAsync();
        var controller = Controller(db, user);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sessions(db, user).StartForRoomAsync(new("UC-101")));
        var result = Assert.IsType<OkObjectResult>(await controller.StartScan(new("UC-101"), default));
        var scan = Assert.IsType<ExamSession>(result.Value);
        var pipeline = Pipeline(db);
        await pipeline.ProcessAsync(new("my-device", "S1", scan.Id.ToString(), "ble-pair", -10, DateTimeOffset.UtcNow));
        Assert.Empty(await db.Alerts.ToListAsync());
        var own = await db.Devices.SingleAsync();
        await controller.Confirm(scan.Id, new([new(own.Id, "Moj telefon")]), default);
        var session = await Sessions(db, user).StartForRoomAsync(new("UC-101"));
        var entry = await db.WhitelistEntries.SingleAsync();
        Assert.Equal(session.Id.ToString(), entry.SessionId); Assert.NotNull(entry.StaffDeviceId);
        await pipeline.ProcessAsync(new("my-device", "S1", session.Id.ToString(), "ble-pair", -10, DateTimeOffset.UtcNow));
        Assert.Empty(await db.Alerts.ToListAsync());
        await pipeline.ProcessAsync(new("unknown-device", "S1", session.Id.ToString(), "wifi", -10, DateTimeOffset.UtcNow));
        Assert.Single(await db.Alerts.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Remove(entry.StaffDeviceId!.Value, default));
        Assert.Single(await Sessions(db, user).GetAllAsync());
    }

    [Fact]
    public async Task Assistant_cannot_claim_unobserved_device_or_access_another_users_scan_or_session()
    {
        await using var db = Database();
        var first = AppUser.Create("first", "Assistant"); var second = AppUser.Create("second", "Assistant");
        db.Users.AddRange(first, second); await db.SaveChangesAsync();
        var controller = Controller(db, first);
        var scan = Assert.IsType<ExamSession>(Assert.IsType<OkObjectResult>(await controller.StartScan(new("UC-101"), default)).Value);
        await Assert.ThrowsAsync<ArgumentException>(() => controller.Confirm(scan.Id, new([new(Guid.NewGuid(), "Foreign")]), default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Controller(db, second).Scan(scan.Id, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Sessions(db, second).StopAsync(scan.Id));
        var groups = await MonitoringAudience.Groups(db, new { sessionId = scan.Id.ToString() });
        Assert.Contains($"user:{first.Id}", groups); Assert.DoesNotContain($"user:{second.Id}", groups);
        Assert.Empty(await db.StaffDevices.ToListAsync());
    }

    [Fact]
    public async Task Expired_scan_ignores_readings_and_does_not_block_room()
    {
        await using var db = Database();
        var user = AppUser.Create("assistant", "Assistant"); db.Users.Add(user);
        var scan = ExamSession.Create("Expired", "UC-101", DateTimeOffset.UtcNow.AddMinutes(-5));
        scan.SetOwner(user.Id); scan.MarkAsRegistrationScan(DateTimeOffset.UtcNow.AddMinutes(-3)); scan.Start(DateTimeOffset.UtcNow.AddMinutes(-5));
        db.ExamSessions.Add(scan); await db.SaveChangesAsync();
        await Pipeline(db).ProcessAsync(new("late", "S1", scan.Id.ToString(), "wifi", -10, DateTimeOffset.UtcNow));
        Assert.Empty(await db.DeviceObservations.ToListAsync());
        Assert.False(await new ExamSessionRepository(db).HasActiveSessionInRoomAsync("UC-101", Guid.Empty));
    }

    [Fact]
    public async Task Staff_revision_prevents_start_using_stale_registration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        var user = AppUser.Create("assistant", "Assistant"); db.Users.Add(user); await db.SaveChangesAsync();
        await using var parallel = new AppDbContext(options);
        var stale = await parallel.Users.SingleAsync();
        user.TouchStaffDevices(); await db.SaveChangesAsync();
        stale.TouchStaffDevices();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => parallel.SaveChangesAsync());
    }
}
