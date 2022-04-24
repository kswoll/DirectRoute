using DirectRoute.Endpoints.Server.Middleware;

namespace DirectRoute.Endpoints.Server;

public class ApiEndpointRequestHandler
{
    private readonly DirectRouteConfiguration configuration;
    private readonly EndpointInitializersBase endpointInitializers;

    public ApiEndpointRequestHandler(DirectRouteConfiguration configuration, EndpointInitializersBase endpointInitializers)
    {
        this.configuration = configuration;
        this.endpointInitializers = endpointInitializers;
    }

    public RequestDelegate HandleRequest(Type endpointType, Action<ApiEndpoint>? initializer = null)
    {
        return async context =>
        {
            var endpoint = (ApiEndpoint?)context.RequestServices.GetService(endpointType);
            if (endpoint == null)
                throw new InvalidOperationException($"No endpoint found for type {endpointType.FullName}");

            initializer?.Invoke(endpoint);
            endpointInitializers.GetInitializer(endpointType)?.Invoke(endpoint);

            await InitializeEndpoint(endpoint, context);
            await endpoint.ExecuteAsync();
        };
    }

    protected virtual async Task InitializeEndpoint(ApiEndpoint endpoint, HttpContext context)
    {
        endpoint.Middleware.Add(new HttpMiddleware(context, configuration));
        await endpoint.InitializeAsync(new HttpApiEndpointContext(context));
    }
}