using Gjubrarko.Application.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace Gjubrarko.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ReportService>();
        services.AddScoped<ModerationService>();
        return services;
    }
}
