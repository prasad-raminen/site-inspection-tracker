using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SiteInspectionTracker.Application.Dashboard;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Application.Sites;

namespace SiteInspectionTracker.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ISiteService, SiteService>();
        services.AddScoped<IInspectionService, InspectionService>();
        services.AddScoped<IFindingService, FindingService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
