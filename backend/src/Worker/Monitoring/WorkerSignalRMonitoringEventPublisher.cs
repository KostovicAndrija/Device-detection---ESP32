using Application.Monitoring;
using Microsoft.AspNetCore.SignalR.Client;

namespace Worker.Monitoring;

public sealed class WorkerSignalRMonitoringEventPublisher(IConfiguration configuration) : IMonitoringEventPublisher
{
    private readonly string _hubUrl = configuration.GetValue<string>("Monitoring:HubUrl") ?? "http://localhost:5000/hubs/monitoring";
    private HubConnection? _connection;

    public async Task PublishDeviceUpdatedAsync(DeviceUpdatedEvent payload, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await connection.InvokeAsync("PublishFromWorker", "deviceUpdated", payload, cancellationToken);
    }

    public async Task PublishAlertCreatedAsync(AlertCreatedEvent payload, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        await connection.InvokeAsync("PublishFromWorker", "alertCreated", payload, cancellationToken);
    }

    private async Task<HubConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            if (_connection.State == HubConnectionState.Connected)
            {
                return _connection;
            }

            await _connection.StartAsync(cancellationToken);
            return _connection;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl)
            .WithAutomaticReconnect()
            .Build();

        await _connection.StartAsync(cancellationToken);
        return _connection;
    }
}
