using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

[Authorize]
public sealed class MonitoringHub : Hub
{
    public Task PublishFromWorker(string eventName, object payload)
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

        return Clients.All.SendAsync(eventName, payload);
    }
}
