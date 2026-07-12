using Application.Ingestion;
using Infrastructure.Ingestion;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IRssiProcessingPipeline, LoggingRssiProcessingPipeline>();
        return services;
    }
}
