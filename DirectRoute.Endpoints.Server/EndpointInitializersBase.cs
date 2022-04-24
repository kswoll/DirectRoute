using System.Collections.Concurrent;

namespace DirectRoute.Endpoints.Server;

/// <summary>
/// Allows you to apply your own logic to help initialize an endpoint.
/// </summary>
public class EndpointInitializersBase
{
    private readonly ConcurrentDictionary<Type, Action<ApiEndpoint>> initializers = new();

    public EndpointInitializersBase()
    {
        RegisterInitializers();
    }

    protected virtual void RegisterInitializers()
    {
    }

    protected void Add<TImplementation>(Action<TImplementation> initializer)
        where TImplementation : ApiEndpoint
    {
        initializers[typeof(TImplementation)] = x => initializer((TImplementation)x);
    }

    public Action<ApiEndpoint>? GetInitializer(Type implementationType)
    {
        initializers.TryGetValue(implementationType, out var result);
        return result;
    }
}