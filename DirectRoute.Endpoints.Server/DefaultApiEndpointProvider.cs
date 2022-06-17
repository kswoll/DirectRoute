namespace DirectRoute.Endpoints.Server;

public class DefaultApiEndpointProvider : IApiEndpointProvider
{
    public Type? MapRouteToImplementation(DirectRouteServerConfiguration configuration, Route route)
    {
        var endpointInterface = route.EndpointType;
        configuration.EndpointImplemenationsByInterfaceType.TryGetValue(endpointInterface, out var endpointImplementation);
        return endpointImplementation;
    }
}
