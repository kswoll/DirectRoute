namespace DirectRoute.Endpoints;

public interface IRoutes
{
    Route? Get(Type endpointType);
    string? RoutePrefix { get; }
    IEnumerable<Route> List { get; }
    DirectRouteConfiguration Configuration { get; }
}