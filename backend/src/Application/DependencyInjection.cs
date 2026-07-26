using Application.Ingestion;
using Application.Alerts;
using Application.Devices;
using Application.Reporting;
using Application.Risk;
using Application.Localization;
using Application.Sessions;
using Application.Whitelist;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IRssiIngestionService, RssiIngestionService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IExamSessionService, ExamSessionService>();
        services.AddScoped<IWhitelistService, WhitelistService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IDeviceQueryService, DeviceQueryService>();
        services.AddScoped<IAlertService, AlertService>();
        return services;
    }
}
