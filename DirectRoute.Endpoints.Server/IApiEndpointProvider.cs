namespace DirectRoute.Endpoints.Server
{
    public interface IApiEndpointProvider
    {
        Type? MapRouteToImplementation(DirectRouteServerConfiguration configuration, Route route);
    }
}
