namespace DirectRoute.Endpoints;

public abstract class RoutesBase
{
    protected abstract void RegisterRoutes();

    public IReadOnlyList<Route> List => routes;
    public string? RoutePrefix { get; set; }

    private readonly List<Route> routes = new();
    private readonly Dictionary<Type, Route> routesByEndpoint = new();

    public RoutesBase()
    {
        RegisterRoutes();
    }

    public RouteModule Module(Action<RouteModule> moduleHandler)
    {
        return Module("", moduleHandler);
    }

    public RouteModule Module(string prefix, Action<RouteModule> moduleHandler)
    {
        var module = new RouteModule(this, prefix);
        moduleHandler(module);
        return module;
    }

    /// <summary>
    /// Gets the route defined for the specified endpoint interface.,
    /// </summary>
    /// <param name="endpointType"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Route Get(Type endpointType)
    {
        if (!routesByEndpoint.TryGetValue(endpointType, out var route))
            throw new InvalidOperationException($"No route found for endpoint: {endpointType}, check that the endpoint is registered in your Routes class");
        return route;
    }

    private Route<T> Route<T>(RouteMethod method, string path)
        where T : IEndpoint
    {
        var route = new Route<T>(this, method, path);
        routes.Add(route);
        routesByEndpoint[typeof(T)] = route;
        return route;
    }

    /// <summary>
    /// Define a route representing an HTTP GET request.
    /// </summary>
    /// <typeparam name="T">The type of your endpoint interface</typeparam>
    /// <param name="path">The path template that describes the URL path for your route</param>
    /// <returns>The newly created Route instance</returns>
    public Route<T> Get<T>(string path) where T : IEndpoint
    {
        return Route<T>(RouteMethod.Get, path);
    }

    /// <summary>
    /// Define a route representing an HTTP POST request.
    /// </summary>
    /// <typeparam name="T">The type of your endpoint interface</typeparam>
    /// <param name="path">The path template that describes the URL path for your route</param>
    /// <returns>The newly created Route instance</returns>
    public Route<T> Post<T>(string path) where T : IEndpoint
    {
        return Route<T>(RouteMethod.Post, path);
    }

    /// <summary>
    /// Define a route representing an HTTP PUT request.
    /// </summary>
    /// <typeparam name="T">The type of your endpoint interface</typeparam>
    /// <param name="path">The path template that describes the URL path for your route</param>
    /// <returns>The newly created Route instance</returns>
    public Route<T> Put<T>(string path) where T : IEndpoint
    {
        return Route<T>(RouteMethod.Put, path);
    }

    /// <summary>
    /// Define a route representing an HTTP DELETE request.
    /// </summary>
    /// <typeparam name="T">The type of your endpoint interface</typeparam>
    /// <param name="path">The path template that describes the URL path for your route</param>
    /// <returns>The newly created Route instance</returns>
    public Route<T> Delete<T>(string path) where T : IEndpoint
    {
        return Route<T>(RouteMethod.Delete, path);
    }
}