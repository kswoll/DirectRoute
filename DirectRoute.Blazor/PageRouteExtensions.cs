using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace DirectRoute.Blazor;

public static class PageRouteExtensions
{
    private static readonly ConditionalWeakTable<ComponentBase, ImmutableList<PageRoute>> pageRoutesByComponent = new();

    /// <summary>
    /// Method specifies a return type that is not void because it's being used in razor files that expect a return value.
    /// </summary>
    public static object? Page(this ComponentBase page, PageRoute route)
    {
        if (!pageRoutesByComponent.TryGetValue(page, out ImmutableList<PageRoute>? routes))
            routes = ImmutableList<PageRoute>.Empty;

        routes = routes.Add(route);
        pageRoutesByComponent.AddOrUpdate(page, routes);

        return null;
    }

    /// <summary>
    /// Gets a list of all the routes defined for the specified page.  Routes are added to this list by calls to the
    /// Page method above.
    /// </summary>
    public static IReadOnlyList<PageRoute> GetRoutes(this ComponentBase page)
    {
        if (pageRoutesByComponent.TryGetValue(page, out ImmutableList<PageRoute>? routes))
            return routes;
        else
            return ImmutableList<PageRoute>.Empty;
    }
}
