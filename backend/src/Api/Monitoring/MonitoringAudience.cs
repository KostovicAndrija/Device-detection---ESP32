using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Monitoring;

public static class MonitoringAudience
{
    public static async Task<IReadOnlyList<string>> Groups(AppDbContext db, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        string? sessionId = null;
        if (json.TryGetProperty("sessionId", out var value)) sessionId = value.ToString();
        else if (json.TryGetProperty("alertId", out var alert) && Guid.TryParse(alert.ToString(), out var alertId))
            sessionId = await db.Alerts.Where(a => a.Id == alertId).Select(a => a.SessionId).SingleOrDefaultAsync(ct);
        if (Guid.TryParse(sessionId, out var id))
        {
            var owner = await db.ExamSessions.Where(s => s.Id == id).Select(s => s.OwnerUserId).SingleOrDefaultAsync(ct);
            if (owner is not null) return ["professors", $"user:{owner}"];
        }
        return ["professors"];
    }
}
