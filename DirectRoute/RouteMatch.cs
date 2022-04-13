namespace DirectRoute;

public readonly struct RouteMatch
{
    public bool IsMatch { get; }
    public IReadOnlyDictionary<string, object> RouteArguments { get; }

    private static readonly Dictionary<string, object> EmptyRouteArguments = new();

    public RouteMatch(bool isMatch = false)
    {
        IsMatch = isMatch;
        RouteArguments = EmptyRouteArguments;
    }

    public RouteMatch(Dictionary<string, object> routeArguments)
    {
        IsMatch = true;
        RouteArguments = routeArguments;
    }

    public static implicit operator RouteMatch(bool isMatch)
    {
        return new RouteMatch(isMatch);
    }
}