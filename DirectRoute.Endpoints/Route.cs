namespace DirectRoute.Endpoints;

public abstract class Route
{
    public IRoutes Routes { get; internal set; }
    public RouteMethod Method { get; }
    public Type EndpointType { get; }
    public IEnumerable<RoutePart> Variables => path.Parts.Where(x => x.Type == RoutePartType.Variable);

    private readonly RoutePath path;

    public Route(IRoutes routes, Type endpointType, RouteMethod method, string routeString)
    {
        Routes = routes;
        EndpointType = endpointType;
        Method = method;
        path = RoutePath.Parse(routeString);
    }

    public IReadOnlyList<RoutePart> PathParts => path.Parts;

    public string Path
    {
        get
        {
            if (Routes.RoutePrefix == null)
                return $"/{path.Value}";
            else
                return $"/{Routes.RoutePrefix}/{path.Value}";
        }
    }

    public string FormatUrl(object routeArguments, object queryString)
    {
        var path = this.path.FormatUrl(routeArguments, queryString, Routes.Configuration.QueryStringOptions);
        if (Routes.RoutePrefix == null)
            return $"/{path}";
        else
            return $"/{Routes.RoutePrefix}/{path}";
    }
}

public class Route<T> : Route where T : IEndpoint
{
    public Route(IRoutes routes, RouteMethod method, string routeString) : base(routes, typeof(T), method, routeString)
    {
    }
}

// The following classes should only be used by routes defined using PropertyRoutesBase.  The missing parameters
// will be filled in later.

public class GetRoute<T> : Route<T> where T : IEndpoint
{
    public GetRoute(string routeString) : base(default!, RouteMethod.Get, routeString)
    {
    }
}

public class PutRoute<T> : Route<T> where T : IEndpoint
{
    public PutRoute(string routeString) : base(default!, RouteMethod.Put, routeString)
    {
    }
}

public class PostRoute<T> : Route<T> where T : IEndpoint
{
    public PostRoute(string routeString) : base(default!, RouteMethod.Post, routeString)
    {
    }
}

public class DeleteRoute<T> : Route<T> where T : IEndpoint
{
    public DeleteRoute(string routeString) : base(default!, RouteMethod.Delete, routeString)
    {
    }
}