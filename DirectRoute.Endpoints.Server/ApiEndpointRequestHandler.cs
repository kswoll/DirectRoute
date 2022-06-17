using DirectRoute.Endpoints.Server.Middleware;

namespace DirectRoute.Endpoints.Server;

public class ApiEndpointRequestHandler
{
    protected readonly DirectRouteServerConfiguration configuration;
    protected readonly EndpointsBase endpoints;

    public ApiEndpointRequestHandler(DirectRouteServerConfiguration configuration, EndpointsBase endpoints)
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

    protected virtual HttpMiddleware CreateMiddleware(ILogger logger, ApiEndpoint endpoint, HttpContext context)
    {
        return new HttpMiddleware(configuration, context);
    }

    protected virtual async Task InitializeEndpoint(ILogger logger, ApiEndpoint endpoint, HttpContext context)
    {
        endpoint.Middleware.Add(CreateMiddleware(logger, endpoint, context));
        await endpoint.InitializeAsync(new HttpApiEndpointContext(configuration, logger, context));
    }
}