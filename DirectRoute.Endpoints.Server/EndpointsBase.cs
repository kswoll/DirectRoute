using System.Collections.Concurrent;

namespace DirectRoute.Endpoints.Server;

public class EndpointsBase : IApiEndpointProvider
{
    private readonly ConcurrentDictionary<Route, (Type Type, Action<ApiEndpoint>? Initializer)> registry = new();

    protected void Add<TImplementation>(Route route, Action<TImplementation>? initializer = null)
        where TImplementation : ApiEndpoint
    {
        registry[route] = (typeof(TImplementation), initializer == null ? null : x => initializer((TImplementation)x));
    }

    public Action<ApiEndpoint>? GetInitializer(Route route)
    {
        if (registry.TryGetValue(route, out var value) && value is var (_, initializer))
            return initializer;
        return null;
    }

    public Type? MapRouteToImplementation(DirectRouteServerConfiguration configuration, Route route)
    {
        if (registry.TryGetValue(route, out var value))
        {
            return value.Type;
        }
        else
        {
            return null;
        }
    }
}