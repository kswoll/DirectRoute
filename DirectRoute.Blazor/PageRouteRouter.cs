using Microsoft.AspNetCore.Components.Routing;
using System.Net;
using System.Reflection;

namespace DirectRoute.Blazor;

public class PageRouteRouter : IComponent, IHandleAfterRender, IDisposable
{
    private RenderHandle renderHandle;
    private bool navigationInterceptionEnabled;
    private string? location;
    private PageRouteTree? routeTree;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private INavigationInterception NavigationInterception { get; set; } = default!;

    [Inject]
    private PageRoutesBase PageRoutes { get; set; } = default!;

    [Parameter]
    public RenderFragment? NotFound { get; set; }

    [Parameter]
    public RenderFragment<RouteData>? Found { get; set; }

    [Parameter]
    public Assembly[]? ReferenceAssemblies { get; set; }

    [Parameter]
    public IPageRouteHandler? PageRouteHandler { get; set; }

    public void Attach(RenderHandle renderHandle)
    {
        this.renderHandle = renderHandle;
        location = NavigationManager.Uri;
        NavigationManager.LocationChanged += HandleLocationChanged;
    }

    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (Found == null)
            throw new InvalidOperationException($"The {nameof(PageRouteRouter)} component requires a value for the parameter {nameof(Found)}.");
        if (NotFound == null)
            throw new InvalidOperationException($"The {nameof(PageRouteRouter)} component requires a value for the parameter {nameof(NotFound)}.");
        if (ReferenceAssemblies == null)
            throw new InvalidOperationException($"The {nameof(PageRouteRouter)} component requires a value for the parameter {nameof(ReferenceAssemblies)}");

        var pageRoutes = ReferenceAssemblies.SelectMany(x => x.GetTypes())
            .Where(x => typeof(ComponentBase).IsAssignableFrom(x))
            .SelectMany(x => x.GetCustomAttributes<RouteAttribute>(), (page, route) => new { Page = page, Route = route })
            .Select(x => new PageRoute(x.Route.Template, x.Page))
            .ToList();

        pageRoutes.AddRange(PageRoutes.Routes);

        routeTree = new PageRouteTree(pageRoutes);

        Refresh();

        return Task.CompletedTask;
    }

    public Task OnAfterRenderAsync()
    {
        if (!navigationInterceptionEnabled)
        {
            navigationInterceptionEnabled = true;
            return NavigationInterception.EnableNavigationInterceptionAsync();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= HandleLocationChanged;
        GC.SuppressFinalize(this);
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        location = args.Location;
        Refresh();
    }

    private void Refresh()
    {
        var path = NavigationManager.ToBaseRelativePath(location!);
        if (path.StartsWith("/"))
            path = path[1..];

        int hashIndex = path.IndexOf('#');
        string? hash = null;
        if (hashIndex != -1)
        {
            hash = path[(hashIndex + 1)..];
            path = path.Substring(0, hashIndex);
        }

        int queryIndex = path.IndexOf('?');
        if (queryIndex != -1)
            path = path[..queryIndex];

        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        var (route, routeValues) = routeTree!.Resolve(path);

        if (route != null)
        {
            var page = route.PageType;
            var match = route.Match(pathSegments, routeValues);
            if (!match.IsMatch)
                throw new Exception($"Resolved a route through the route tree, but the route itself considers itself not a match.  Route {route.Path} at {page.FullName}.  (This should never happen)");
            var routeData = new RouteData(page, match.RouteArguments);

            var pageStatusCode = PageRouteHandler?.GetPageStatusCode(page, route, routeData);
            if (pageStatusCode == null || pageStatusCode == HttpStatusCode.Found)
            {
                Console.WriteLine($"Navigating to {path} at {page.FullName}");
                renderHandle.Render(Found!(routeData));
                return;
            }
        }

        // Not found
        renderHandle.Render(NotFound!);
    }
}