using DirectRoute.Endpoints.Server.Middleware;

namespace DirectRoute.Endpoints.Server;

public class ApiEndpointRequestHandler
{
    private readonly DirectRouteConfiguration configuration;

    public ApiEndpointRequestHandler(DirectRouteConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public RequestDelegate HandleRequest(Type endpointType, Action<ApiEndpoint>? initializer = null)
    {
        return async context =>
        {
            var endpoint = (ApiEndpoint?)context.RequestServices.GetService(endpointType);
            if (endpoint == null)
                throw new InvalidOperationException($"No endpoint found for type {endpointType.FullName}");

            initializer?.Invoke(endpoint);

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