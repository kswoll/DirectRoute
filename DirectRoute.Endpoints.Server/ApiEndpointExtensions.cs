using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace DirectRoute.Endpoints.Server;

public delegate Type GenericEndpointImplementationProvider(Type endpointInterface, Type genericImplementation);

public static class ApiEndpointExtensions
{
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
        var configuration = endpoints.ServiceProvider.GetRequiredService<DirectRouteServerConfiguration>();

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
            RouteMethod.Post => endpoints.MapPost(pattern, requestHandler.HandleRequest(route, endpointImplementationType)),
            RouteMethod.Put => endpoints.MapPut(pattern, requestHandler.HandleRequest(route, endpointImplementationType)),
            RouteMethod.Delete => endpoints.MapDelete(pattern, requestHandler.HandleRequest(route, endpointImplementationType)),
            _ => endpoints.MapGet(pattern, requestHandler.HandleRequest(route, endpointImplementationType))
        };

        return endpointImplementationType;
    }

    public static T Set<T, TValue>(this T endpoint, Expression<Func<T, TValue>> property, TValue value)
    {
        var propertyInfo = property.GetPropertyInfo();
        if (propertyInfo == null)
            throw new InvalidOperationException($"Unable to map {property} to a PropertyInfo");

        propertyInfo.SetValue(endpoint, value);
        return endpoint;
    }

    internal static PropertyInfo? GetPropertyInfo(this LambdaExpression expression)
    {
        var current = expression.Body;
        if (current is UnaryExpression unary)
            current = unary.Operand;
        var call = current as MemberExpression;
        return (PropertyInfo?)call?.Member;
    }

    internal static ApiEndpointRequestHandler GetRequestHandler<T>(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.ServiceProvider.GetRequiredService<ApiEndpointRequestHandler>();
    }

    public static async Task<T> TimeWith<T>(this Task<T> task, ApiResponseSpan span, string name)
    {
        T result;
        using (span.Time(name))
        {
            result = await task;
        }
        return result;
    }
}