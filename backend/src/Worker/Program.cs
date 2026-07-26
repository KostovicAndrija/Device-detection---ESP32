using Application;
using Application.Monitoring;
using Infrastructure;
using Worker;
using Worker.Monitoring;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IMonitoringEventPublisher, WorkerSignalRMonitoringEventPublisher>();
builder.Services.AddHostedService<MqttWorker>();

var host = builder.Build();
host.Run();
