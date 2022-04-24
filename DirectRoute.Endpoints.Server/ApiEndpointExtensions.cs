using System.Collections.Concurrent;

namespace DirectRoute.Endpoints.Server;

public delegate Type GenericEndpointImplementationProvider(Type endpointInterface, Type genericImplementation);

public static class ApiEndpointExtensions
{
    public static ApiEndpointModule Module(this IEndpointRouteBuilder endpoints, string prefix, Action<ApiEndpointModule> moduleHandler)
    {
        var module = new ApiEndpointModule(endpoints, prefix);
        moduleHandler(module);
        return module;
    }

    public static IEndpointConventionBuilder MapGet<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : ApiEndpoint
    {
        var result = endpoints.MapGet(pattern, endpoints.GetRequestDelegate<T>());
        return result;
    }

    public static IEndpointConventionBuilder MapPost<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : ApiEndpoint
    {
        var result = endpoints.MapPost(pattern, endpoints.GetRequestDelegate<T>());
        return result;
    }

    public static IEndpointConventionBuilder MapPut<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : ApiEndpoint
    {
        var result = endpoints.MapPut(pattern, endpoints.GetRequestDelegate<T>());
        return result;
    }

    public static IEndpointConventionBuilder MapDelete<T>(this IEndpointRouteBuilder endpoints, string pattern)
        where T : ApiEndpoint
    {
        var result = endpoints.MapDelete(pattern, endpoints.GetRequestDelegate<T>());
        return result;
    }

    /// <summary>
    /// Called internallly by DirectRouteExtensions.MapDirectRoute
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="route"></param>
    /// <param name="genericImplementationProvider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    internal static Type MapRoute(this IEndpointRouteBuilder endpoints, Route route)
    {
        var endpointProviders = endpoints.ServiceProvider.GetServices<IApiEndpointProvider>().ToArray();
        var configuration = endpoints.ServiceProvider.GetRequiredService<DirectRouteConfiguration>();

        var endpointInterface = route.EndpointType;

        Type? endpointImplementationType = null;
        foreach (var endpointProvider in endpointProviders)
        {
            endpointImplementationType = endpointProvider.MapRouteToImplementation(configuration, route);
            if (endpointImplementationType != null)
                break;
        }
        if (endpointImplementationType == null)
            throw new Exception($"Unable to find an implementation for interface {endpointInterface.FullName}.");

        var requestHandler = endpoints.ServiceProvider.GetRequiredService<ApiEndpointRequestHandler>();
        var pattern = route.Path;

        _ = route.Method switch
        {
            RouteMethod.Post => endpoints.MapPost(pattern, requestHandler.HandleRequest(endpointImplementationType)),
            RouteMethod.Put => endpoints.MapPut(pattern, requestHandler.HandleRequest(endpointImplementationType)),
            RouteMethod.Delete => endpoints.MapDelete(pattern, requestHandler.HandleRequest(endpointImplementationType)),
            _ => endpoints.MapGet(pattern, requestHandler.HandleRequest(endpointImplementationType))
        };

        return endpointImplementationType;
    }

    internal static ApiEndpointRequestHandler GetRequestHandler<T>(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.ServiceProvider.GetRequiredService<ApiEndpointRequestHandler>();
    }

    internal static RequestDelegate GetRequestDelegate<T>(this IEndpointRouteBuilder endpoints, Action<ApiEndpoint>? initializer = null)
    {
        return endpoints.GetRequestHandler<T>().HandleRequest(typeof(T), initializer);
    }
}