using Microsoft.AspNetCore.SignalR;

namespace Api.Hubs;

public sealed class MonitoringHub : Hub
{
	public Task PublishFromWorker(string eventName, object payload)
		=> Clients.All.SendAsync(eventName, payload);
}
