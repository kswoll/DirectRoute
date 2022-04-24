namespace DirectRoute.Endpoints.Server
{
    public interface IApiEndpointProvider
    {
        Type? MapRouteToImplementation(DirectRouteConfiguration configuration, Route route);
    }
}
