using Application.Abstractions.Security;
using Application.Sessions;
using Domain.Entities;
using Infrastructure.Monitoring;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests;

public sealed class SessionDeletionTests
{
    private sealed record Actor(Guid? Id, bool IsProfessor, bool IsAssistant) : ICurrentUser;

    private static AppDbContext Database()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static ExamSessionService Service(AppDbContext db, ICurrentUser actor) => new(
        new ExamSessionRepository(db),
        new AppUnitOfWork(db),
        new NoopMonitoringEventPublisher(),
        new SessionAccess(db, actor),
        new RoomLayoutProvider(db));

    [Fact]
    public async Task Stopping_empty_session_removes_it_instead_of_saving_history()
    {
        await using var db = Database();
        var session = ExamSession.Create("Prazna", "UC-101", DateTimeOffset.UtcNow);
        session.Start(DateTimeOffset.UtcNow);
        db.ExamSessions.Add(session);
        await db.SaveChangesAsync();

        var result = await Service(db, new Actor(Guid.NewGuid(), true, false)).StopAsync(session.Id);

        Assert.NotNull(result);
        Assert.True(result.Deleted);
        Assert.Null(result.Session);
        Assert.Empty(await db.ExamSessions.ToListAsync());
    }

    [Fact]
    public async Task Stopping_session_with_an_observation_keeps_completed_history()
    {
        await using var db = Database();
        var now = DateTimeOffset.UtcNow;
        var session = ExamSession.Create("Sa uređajem", "UC-101", now);
        session.Start(now);
        var device = Device.Create("device-hash", now, "wifi");
        db.AddRange(session, device);
        db.DeviceObservations.Add(DeviceObservation.Create(device.Id, "S1", session.Id.ToString(), "wifi", -55, now));
        await db.SaveChangesAsync();

        var result = await Service(db, new Actor(Guid.NewGuid(), true, false)).StopAsync(session.Id);

        Assert.NotNull(result);
        Assert.False(result.Deleted);
        Assert.Equal("completed", result.Session?.Status);
        Assert.Single(await db.ExamSessions.ToListAsync());
    }

    [Fact]
    public async Task Only_professor_can_delete_session_and_related_records_are_removed()
    {
        await using var db = Database();
        var now = DateTimeOffset.UtcNow;
        var session = ExamSession.Create("Za brisanje", "UC-101", now);
        session.Start(now);
        var device = Device.Create("delete-device", now, "bluetooth");
        db.AddRange(session, device);
        var sessionId = session.Id.ToString();
        db.DeviceObservations.Add(DeviceObservation.Create(device.Id, "S1", sessionId, "bluetooth", -40, now));
        db.Alerts.Add(Alert.Create(device.Id, sessionId, 80, "test", now));
        db.WhitelistEntries.Add(WhitelistEntry.Create(sessionId, "student", "delete-device", now, null));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Service(db, new Actor(Guid.NewGuid(), false, true)).DeleteAsync(session.Id));

        Assert.True(await Service(db, new Actor(Guid.NewGuid(), true, false)).DeleteAsync(session.Id));
        Assert.Empty(await db.ExamSessions.ToListAsync());
        Assert.Empty(await db.DeviceObservations.ToListAsync());
        Assert.Empty(await db.Alerts.ToListAsync());
        Assert.Empty(await db.WhitelistEntries.ToListAsync());
        Assert.Single(await db.Devices.ToListAsync());
    }
}
