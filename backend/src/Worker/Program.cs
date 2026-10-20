using Application;
using Application.Monitoring;
using Infrastructure;
using Worker;
using Worker.Monitoring;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// One persistent hub connection is shared by all MQTT messages. A scoped publisher
// would create and leak a SignalR connection for every observation.
builder.Services.AddSingleton<IMonitoringEventPublisher, WorkerSignalRMonitoringEventPublisher>();
builder.Services.AddSingleton<WorkerAccessTokenProvider>();
builder.Services.AddHostedService<MqttWorker>();

var host = builder.Build();
host.Run();
