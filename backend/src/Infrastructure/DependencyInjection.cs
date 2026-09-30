using Application.Ingestion;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Monitoring;
using Infrastructure.Monitoring;
using Infrastructure.Ingestion;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HashingOptions>(configuration.GetSection(HashingOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
        });

        services.AddScoped<IAppUnitOfWork, AppUnitOfWork>();
        services.AddScoped<ICurrentUser, AnonymousCurrentUser>();
        services.AddScoped<ISessionAccess, SessionAccess>();
        services.AddScoped<Application.Localization.IRoomLayoutProvider, RoomLayoutProvider>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IObservationRepository, ObservationRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
        services.AddScoped<IWhitelistRepository, WhitelistRepository>();
        services.AddScoped<IDeviceHashingService, Sha256DeviceHashingService>();
        services.AddScoped<IMonitoringEventPublisher, NoopMonitoringEventPublisher>();
        services.AddScoped<IRssiProcessingPipeline, LoggingRssiProcessingPipeline>();

        return services;
    }
}
