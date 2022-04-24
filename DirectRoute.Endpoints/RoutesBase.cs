using System.Collections.Concurrent;

namespace DirectRoute.Endpoints;

public class RoutesBase : IRoutes
{
    public string? RoutePrefix { get; }

    private readonly RoutesBase? parent;
    private readonly List<Route> routes = new();
    private readonly Dictionary<Type, Route> routesByEndpoint = new();
    private readonly List<RoutesBase> modules = new();
    private readonly ConcurrentDictionary<Type, Route?> routesByEndpointCache = new();

    public RoutesBase(string? routePrefix)
    {
        RoutePrefix = routePrefix;
    }

    private RoutesBase(RoutesBase parent, string? routePrefix)
    {
        this.parent = parent;
        RoutePrefix = routePrefix;
    }

    public IEnumerable<Route> List => routes.Concat(modules.SelectMany(x => x.List));

    /// <summary>
    /// Gets the route defined for the specified endpoint interface.
    /// </summary>
    public Route? Get(Type endpointType)
    {
        return routesByEndpointCache.GetOrAdd(endpointType, type =>
        {
            if (routesByEndpoint.TryGetValue(type, out var route))
            {
                return route;
            }
            else
            {
                foreach (var module in modules)
                {
                    route = module.Get(type);
                    if (route != null)
                        return route;
                }

                return null;
            }
        });

    }

    protected void Register(Route route)
    {
        routes.Add(route);
        routesByEndpoint[route.EndpointType] = route;
    }

    private string DeriveSubPrefix(string prefix)
    {
        if (string.IsNullOrEmpty(RoutePrefix))
            return prefix;
        else if (string.IsNullOrEmpty(prefix))
            return RoutePrefix;
        else
            return $"{RoutePrefix}/{prefix}";
    }

    // TODO: convert these to mixins (extension methods)
    #region RouteList behavior

    public RoutesBase Module(Action<RoutesBase> moduleHandler)
    {
        return Module("", moduleHandler);
    }

    public RoutesBase Module(string prefix, Action<RoutesBase> moduleHandler)
    {
        var module = new RoutesBase(this, DeriveSubPrefix(prefix));
        moduleHandler(module);
        modules.Add(module);
        return module;
    }

    private Route<T> Route<T>(RouteMethod method, string path)
        where T : IEndpoint
    {
        var route = new Route<T>(this, method, path);
        Register(route);
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

    #endregion

    #region PropertyRoutes behavior

    /// <summary>
    /// Assigns the Routes property to each route property defined in in this class.  Call this from your
    /// constructor after initializing RoutePrefix.  Also registers them.
    /// </summary>
    protected void InitializeRoutes()
    {
        foreach (var property in GetType().GetProperties().Where(x => typeof(Route).IsAssignableFrom(x.PropertyType)))
        {
            var route = (Route?)property.GetValue(this);
            if (route != null)
            {
                route.Routes = this;
                Register(route);
            }
        }
    }

    #endregion
}
