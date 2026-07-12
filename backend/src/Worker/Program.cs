using Application;
using Infrastructure;
using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddHostedService<MqttWorker>();

var host = builder.Build();
host.Run();
