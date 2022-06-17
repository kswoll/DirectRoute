using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DirectRoute.Blazor;

public static class DirectRouteExtensions
{
    /// <summary>
    /// Installs DirectRoute in a Blazor app
    /// </summary>
    /// <param name="services">The services collection</param>
    /// <param name="pageRoutes">Your subclass of PageRoutesBase</param>
    public static void AddDirectRoute(this IServiceCollection services, PageRoutesBase pageRoutes, JsonSerializerOptions jsonOptions, QueryStringOptions? queryStringOptions = null)
    {
        services.AddSingleton(pageRoutes);
        services.AddSingleton(new DirectRouteConfiguration(jsonOptions, queryStringOptions));
    }
}