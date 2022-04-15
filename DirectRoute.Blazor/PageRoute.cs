namespace DirectRoute.Blazor;

public class PageRoute
{
    public IEnumerable<RoutePart> Variables => path.Parts.Where(x => x.Type == RoutePartType.Variable);
    public Type PageType { get; }

    private readonly RoutePath path;

    public string Path => path.Value;

    public string ToUrl(params object[] routeValues) => $"/{path.FormatUrl(routeValues, null)}";

    public string FormatUrl(object? routeArguments = null, object? queryString = null) => path.FormatUrl(routeArguments, queryString);
    public IReadOnlyList<RoutePart> Parts => path.Parts;

    public PageRoute(string routeString, Type pageType)
    {
        path = RoutePath.Parse(routeString);
        PageType = pageType;
    }

    public RouteMatch Match(string[] pathSegments, object[] routeValues)
    {
        return path.Match(pathSegments, routeValues);
    }
}

public class PageRoute<TPage> : PageRoute
    where TPage : ComponentBase
{
    public PageRoute(string routeString) : base(routeString, typeof(TPage))
    {
    }
}