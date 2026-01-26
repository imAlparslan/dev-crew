using DevCrew.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddDevCrewCore(this IServiceCollection services)
    {
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddSingleton<IGuidService, GuidService>();
        return services;
    }
}
