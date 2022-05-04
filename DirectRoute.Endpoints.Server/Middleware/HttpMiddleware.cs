using DirectRoute.TypeConverters;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Reflection;

namespace DirectRoute.Endpoints.Server.Middleware;

/// <summary>
/// Enables the the endpoint to handle and respond to HTTP requests.
/// </summary>
public class HttpMiddleware : ApiEndpointMiddleware
{
    private HttpContext Context { get; }

    private readonly DirectRouteConfiguration configuration;

    private PropertyInfo? bodyProperty;

    public HttpMiddleware(HttpContext context, DirectRouteConfiguration configuration)
    {
        Context = context;
        this.configuration = configuration;
    }

    protected override async Task InitializeAsync(ApiEndpoint endpoint)
    {
        BindEndpointMethod(endpoint);

        await BindPropertiesAsync(endpoint);
    }

    protected override void HandleNotFound(ApiEndpoint endpoint)
    {
        Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
    }

    protected override void HandleUnauthorized(ApiEndpoint endpoint)
    {
        Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
    }

    protected override async Task HandleBadRequestAsync(ApiEndpoint endpoint, IReadOnlyList<ValidationFailure> failures)
    {
        Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        var response = new { Failures = failures };
        await Context.Response.WriteAsJsonAsync(response);
    }

    protected override async Task WriteResponseAsync(object? response)
    {
        if (response == null)
        {
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
        else
        {
            Context.Response.StatusCode = (int)HttpStatusCode.OK;
            Context.Response.ContentType = "application/json";

            // Support polymorphic serialization.  By default, types are serialized according to their compile time type.  Furthermore,
            // collections declared as a collection of a base type will not serialize subclass properties.  This solution here only solves
            // root level arrays, but most of the time that's good enough.
            if (response is Array array)
                await Context.Response.WriteAsJsonAsync<object>(array.Cast<object>().ToArray(), configuration.JsonOptions);
            else
                await Context.Response.WriteAsJsonAsync(response, configuration.JsonOptions);
        }
    }

    /// <summary>
    /// Binds all public properties with values from the request.
    /// </summary>
    protected virtual async Task BindPropertiesAsync(ApiEndpoint endpoint)
    {
        // Get properties that should be bound, ignoring properties that have no public setter
        var properties = endpoint.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.GetSetMethod() != null && x.GetSetMethod()!.IsPublic);

        var query = Context.Request.Query.ToDictionary(x => x.Key.ToLower(), x => x.Value.ToArray());
        foreach (var property in properties)
        {
            var name = property.Name.ToLower();
            var value = Context.Request.RouteValues[property.Name];

            if (value == null && query.ContainsKey(name))
                value = query[name];

            // If we still haven't found the value and the property exposes one of [FromBody] or [Body] on either the property or the api method parameter
            if (value == null && (property.GetCustomAttribute<FromBodyAttribute>() != null || property.GetCustomAttribute<BodyAttribute>() != null || property.Name == bodyProperty?.Name))
            {
                var contentType = Context.Request.ContentType;

                if (contentType != null && contentType.StartsWith("application/octet-stream"))
                {
                    if (property.PropertyType == typeof(Stream))
                        property.SetValue(endpoint, Context.Request.Body);
                    else
                        throw new InvalidOperationException($"Unsupported property ({property.Name}) type ({property.PropertyType.FullName}) on {property.DeclaringType!.FullName} for content type application/octet-stream");
                }
                else if (contentType != null && contentType.StartsWith("application/json"))
                {
                    // Commented this out for now since it's possible for there to be a [Body] attribute that is null in
                    // which case we don't transmit any content in the request body.
//                    if (contentType == null || !contentType.StartsWith("application/json"))
//                        throw new InvalidOperationException("When using [FromBody] or [Body] the content type of the request must be application/json");

                    var body = await Context.Request.ReadFromJsonAsync(property.PropertyType);
                    property.SetValue(endpoint, body);
                }
            }

            // If it's a nested query string property of the form key.subkey=subkeyvalue
            if (value == null && query.Keys.Any(x => x.StartsWith($"{name}.")))
            {
                value = BindQueryStringObject(name, property.PropertyType, query);
            }

            if (value != null)
            {
                try
                {
                    var typedValue = TypeConverter.Convert(value, property.PropertyType);

                    property.SetValue(endpoint, typedValue);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error binding value to {property.DeclaringType!.GetFullFriendlyName()}.{property.Name}.  Value: {value}", ex);
                }
            }
        }
    }

    private static object BindQueryStringObject(string parent, Type propertyType, Dictionary<string, string[]> query)
    {
        var result = Activator.CreateInstance(propertyType);
        if (result == null)
            throw new InvalidOperationException($"Null result from Activator.CreateInstance. {propertyType.FullName} is probably a nullable value type");

        foreach (var property in propertyType.GetProperties())
        {
            var name = property.Name.ToLower();
            var key = $"{parent}.{name}";
            if (query.TryGetValue(key, out var value))
            {
                var typedValue = TypeConverter.Convert(value, property.PropertyType);
                property.SetValue(result, typedValue);
            }
        }
        return result;
    }

    /// <summary>
    /// If this ApiEndpoint implements a subtype of interface IEndpoint, it will automatically implement
    /// the Invoke method via Xo.SourceGenerators.  In order to confine the API definition to the interface,
    /// it's important for the [Body] attribute to be applied to the interface method parameter.  To save
    /// you the trouble of repeating that in your ApiEndpoint subclass property equivalent, we scan the
    /// endpoint interface for the [Body] attribute and use it accordingly.
    /// </summary>
    private void BindEndpointMethod(ApiEndpoint endpoint)
    {
        // If this endpoint implements an endpoint interface, we will use attributes on that method and its parameters (for
        // example [Body])
        configuration.EndpointInterfacesByImplementationType.TryGetValue(endpoint.GetType(), out var endpointInterface);
        if (endpointInterface != null)
        {
            var invokeMethod = endpointInterface.GetMethod("Invoke");
            if (invokeMethod == null)
                throw new InvalidOperationException("Invalid endpoint interface.  Must declare a method named Invoke");

            var bodyParameter = invokeMethod.GetParameters().SingleOrDefault(x => x.GetCustomAttribute<BodyAttribute>() != null);
            if (bodyParameter != null)
            {
                var bodyPropertyName = bodyParameter.Name!.Capitalize();
                bodyProperty = endpoint.GetType().GetProperty(bodyPropertyName);
                if (bodyProperty == null)
                    throw new InvalidOperationException($"Endpoint interface {endpointInterface.FullName} defined parameter {bodyParameter.Name} with attribute [Body] but no property with name '{bodyPropertyName}' was found in ApiEndpoint {GetType().FullName}");
            }
        }
    }
}