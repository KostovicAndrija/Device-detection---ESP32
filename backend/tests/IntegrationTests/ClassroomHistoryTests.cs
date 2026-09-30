using Api.Controllers;
using Application.Abstractions.Security;
using Application.Localization;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTests;

public sealed class ClassroomHistoryTests
{
    private sealed record Actor(Guid? Id, bool IsProfessor = false, bool IsAssistant = true) : ICurrentUser;
    private static AppDbContext Db()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Database.EnsureCreated(); return db;
    }
    private static PositionQueryService Query(AppDbContext db, IPositionEstimator? estimator = null) => new(
        new ObservationRepository(db), new DeviceRepository(db), new WhitelistRepository(db),
        estimator ?? new RssiPositionEstimator(new LocalizationService()), new RoomLayoutProvider(db), Options.Create(new LocalizationOptions()));

    [Fact]
    public async Task Session_keeps_geometry_and_sensor_calibration_after_room_changes()
    {
        await using var db = Db(); var layouts = new RoomLayoutProvider(db);
        var session = ExamSession.Create("History", "UC-101", DateTimeOffset.UtcNow);
        await layouts.CaptureAsync(session); session.Start(DateTimeOffset.UtcNow); db.ExamSessions.Add(session); await db.SaveChangesAsync();
        var room = await db.Classrooms.SingleAsync(r => r.Id == "UC-101");
        room.Update(room.Layout() with { Width = 20, Sensors = [new("NEW", 10, 2, "Other sensor", -60, 3.5)] });
        await db.SaveChangesAsync();
        var original = await layouts.ForSessionAsync(session.Id);
        Assert.Equal(8, original.Width); Assert.Equal(3, original.Sensors.Count); Assert.Equal(-45, original.Sensors[0].ReferenceRssi);
        var next = ExamSession.Create("Next", "UC-101", DateTimeOffset.UtcNow); await layouts.CaptureAsync(next);
        db.ExamSessions.Add(next); await db.SaveChangesAsync();
        Assert.Equal("NEW", (await layouts.ForSessionAsync(next.Id)).Sensors[0].Id);
    }

    [Fact]
    public async Task Replay_is_deterministic_ignores_future_observations_and_is_read_only()
    {
        await using var db = Db(); var at = DateTimeOffset.UtcNow.AddDays(-2);
        var session = ExamSession.Create("History", "UC-101", at); await new RoomLayoutProvider(db).CaptureAsync(session);
        session.Start(at); session.Stop(at.AddMinutes(1)); db.ExamSessions.Add(session);
        var device = Device.Create("hash", at, "wifi"); db.Devices.Add(device);
        foreach (var (sensor,rssi) in new[] { ("S1",-60d), ("S2",-65d), ("S3",-63d) })
            db.DeviceObservations.Add(DeviceObservation.Create(device.Id,sensor,session.Id.ToString(),"wifi",rssi,at));
        db.DeviceObservations.Add(DeviceObservation.Create(device.Id,"S1",session.Id.ToString(),"bluetooth_pairing",-20,at.AddSeconds(10)));
        await db.SaveChangesAsync();
        var query = Query(db);
        var first = Assert.Single(await query.AtAsync(session.Id.ToString(),at));
        Assert.Equal("wifi",first.SignalType); Assert.Equal(3,first.SensorCount);
        await query.AtAsync(session.Id.ToString(),at.AddSeconds(12));
        Assert.Equal(first, Assert.Single(await query.AtAsync(session.Id.ToString(),at)));
        Assert.Empty(await query.AtAsync(session.Id.ToString(),at.AddMilliseconds(-1)));
        Assert.Empty(await query.AtAsync(session.Id.ToString(),at.AddSeconds(60)));
        Assert.Equal(4,await db.DeviceObservations.CountAsync()); Assert.Empty(await db.Alerts.ToListAsync());
    }

    [Fact]
    public async Task Assistant_cannot_read_another_sessions_history_layout_or_export()
    {
        await using var db = Db();var owner=AppUser.Create("owner","Assistant");db.Users.Add(owner);
        var session=ExamSession.Create("Private","UC-101",DateTimeOffset.UtcNow);session.SetOwner(owner.Id);db.ExamSessions.Add(session);await db.SaveChangesAsync();
        var controller = new SessionHistoryController(db,new SessionAccess(db,new Actor(Guid.NewGuid())),new RoomLayoutProvider(db),Query(db),new RssiPositionEstimator(new LocalizationService()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>controller.History(session.Id,default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>controller.Layout(session.Id,default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>controller.Frame(session.Id,DateTimeOffset.UtcNow,default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>controller.Observations(session.Id));
    }

    [Fact]
    public async Task Professor_can_save_new_room_and_stale_update_is_rejected()
    {
        await using var db=Db();var controller=new ClassroomsController(db);
        var layout=new RoomLayout("NEW-ROOM","Nova sala","",12,8,[new("PC-1",5,4)],[new("custom-sensor",1,2,"Replacement",-50,3)]);
        var created=Assert.IsType<OkObjectResult>(await controller.Create(layout,default));
        var saved=Assert.IsType<RoomLayout>(created.Value);
        await controller.Update(layout.Id,saved with { Name="Promenjeno" },default);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>controller.Update(layout.Id,saved,default));
        Assert.Equal(2,(await db.Classrooms.SingleAsync(r=>r.Id==layout.Id)).Revision);
    }

    private sealed class TestEstimator : IPositionEstimator
    {
        public string Version=>"test-ai-v1";
        public Task<LocationEstimateDto?> EstimateAsync(RoomLayout room,IReadOnlyCollection<SensorReadingDto> readings,CancellationToken ct=default)
            =>Task.FromResult<LocationEstimateDto?>(new(2,3,.9));
    }
    [Fact]
    public async Task Estimator_can_be_replaced_without_changing_ingestion_or_storage()
    {
        await using var db=Db();var at=DateTimeOffset.UtcNow;
        var session=ExamSession.Create("AI adapter","UC-101",at);await new RoomLayoutProvider(db).CaptureAsync(session);db.ExamSessions.Add(session);
        var device=Device.Create("ai-hash",at,"ble");db.Devices.Add(device);
        db.DeviceObservations.Add(DeviceObservation.Create(device.Id,"S1",session.Id.ToString(),"ble",-60,at));await db.SaveChangesAsync();
        var result=Assert.Single(await Query(db,new TestEstimator()).AtAsync(session.Id.ToString(),at));
        Assert.Equal(2,result.X);Assert.Equal(3,result.Y);Assert.Equal("bluetooth",result.SignalType);
    }
}
