using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Api.Monitoring;
using Infrastructure.Persistence;
using System.Security.Claims;

namespace Api.Hubs;

[Authorize]
public sealed class MonitoringHub(AppDbContext db) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Professor") == true) await Groups.AddToGroupAsync(Context.ConnectionId, "professors");
        else if (Context.User?.IsInRole("Assistant") == true && Guid.TryParse(Context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{id}");
        await base.OnConnectedAsync();
    }

    public async Task PublishFromWorker(string eventName, object payload)
    {
        if (!Context.User?.IsInRole("Worker") ?? true)
        {
            throw new HubException("Only the ingestion worker can publish monitoring events.");
        }

        var allowedEvents = new HashSet<string>(StringComparer.Ordinal)
        {
            "deviceUpdated",
            "alertCreated",
            "alertAcknowledged",
            "sessionStateChanged"
        };
        if (!allowedEvents.Contains(eventName))
        {
            throw new HubException("Unsupported monitoring event.");
        }

        await Clients.Groups(await MonitoringAudience.Groups(db, payload)).SendAsync(eventName, payload);
    }
}
