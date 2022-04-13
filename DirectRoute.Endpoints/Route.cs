namespace DirectRoute.Endpoints;

public abstract class Route
{
    public RoutesBase Routes { get; }
    public RouteMethod Method { get; set; }
    public Type EndpointType { get; }
    public IEnumerable<RoutePart> Variables => path.Parts.Where(x => x.Type == RoutePartType.Variable);

    private readonly RoutePath path;

    public Route(RoutesBase routes, Type endpointType, RouteMethod method, string routeString)
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
        var path = this.path.FormatUrl(routeArguments, queryString);
        if (Routes.RoutePrefix == null)
            return $"/{path}";
        else
            return $"/{Routes.RoutePrefix}/{path}";
    }
}

public class Route<T> : Route where T : IEndpoint
{
    public Route(RoutesBase routes, RouteMethod method, string routeString) : base(routes, typeof(T), method, routeString)
    {
    }
}