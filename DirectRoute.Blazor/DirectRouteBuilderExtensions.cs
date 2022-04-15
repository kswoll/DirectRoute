using Microsoft.Extensions.DependencyInjection;

namespace DirectRoute.Blazor;

public static class DirectRouteBuilderExtensions
{
    /// <summary>
    /// Installs DirectRoute in a Blazor app
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="pageRoutes">Your subclass of PageRoutesBase</param>
    public static void AddDirectRoute(this IServiceCollection services, PageRoutesBase pageRoutes)
    {
        services.AddSingleton(pageRoutes);
    }
}