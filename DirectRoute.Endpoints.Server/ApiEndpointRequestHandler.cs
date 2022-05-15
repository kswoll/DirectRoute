using DirectRoute.Endpoints.Server.Middleware;

namespace DirectRoute.Endpoints.Server;

public class ApiEndpointRequestHandler
{
    private readonly DirectRouteConfiguration configuration;
    private readonly EndpointsBase endpoints;

    public ApiEndpointRequestHandler(DirectRouteConfiguration configuration, EndpointsBase endpoints)
    {
        this.configuration = configuration;
        this.endpoints = endpoints;
    }

    public RequestDelegate HandleRequest(Route route, Type endpointType, Action<ApiEndpoint>? initializer = null)
    {
        return async context =>
        {
            var endpoint = (ApiEndpoint?)context.RequestServices.GetService(endpointType);
            if (endpoint == null)
                throw new InvalidOperationException($"No endpoint found for type {endpointType.FullName}");

            initializer?.Invoke(endpoint);
            endpoints.GetInitializer(route)?.Invoke(endpoint);

            var loggerType = typeof(ILogger<>).MakeGenericType(endpointType);
            var logger = (ILogger)context.RequestServices.GetRequiredService(loggerType);

            await InitializeEndpoint(logger, endpoint, context);
            await endpoint.ExecuteAsync();
        };
    }

    protected virtual async Task InitializeEndpoint(ILogger logger, ApiEndpoint endpoint, HttpContext context)
    {
        endpoint.Middleware.Add(new HttpMiddleware(context, configuration));
        await endpoint.InitializeAsync(new HttpApiEndpointContext(logger, context));
    }
}