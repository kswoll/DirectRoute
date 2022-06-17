using System.Security.Claims;

namespace DirectRoute.Endpoints.Server;

/// <summary>
/// ApiEndpoint instances created within the context of handling a Web Api route will use this as its
/// context, providing the HttpContext to HttpMiddleware, which handles the actual logic to service
/// the request through the endpoint.
/// </summary>
public class HttpApiEndpointContext : IApiEndpointContext
{
    public DirectRouteConfiguration Configuration { get; set; }
    public ILogger Logger { get; }
    public HttpContext Context { get; }
    public ClaimsPrincipal CurrentUser => Context.User;

    public HttpApiEndpointContext(DirectRouteConfiguration configuration, ILogger logger, HttpContext context)
    {
        Configuration = configuration;
        Logger = logger;
        Context = context;
    }
}