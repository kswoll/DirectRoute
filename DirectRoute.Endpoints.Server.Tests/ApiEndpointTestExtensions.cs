using DirectRoute.Endpoints.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Text;
using System.Text.Json;

namespace DirectRoute.Endpoints.Server.Tests;

public static class ApiEndpointTestExtensions
{
    public static async Task<T> Call<T>(this ApiEndpoint endpoint, object? body, object? query, object? routeValues)
    {
        // Pre request body logic
        var features = new FeatureCollection();

        if (query != null)
        {
            var queryString = query.ToDictionary().ToDictionary(x => x.Key, x => new StringValues(x.Value?.ToString()));
            var queryCollection = new QueryCollection(queryString);
            var queryFeature = new QueryFeature(queryCollection);
            features.Set<IQueryFeature>(queryFeature);
        }
        else
        {
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues>());
            var queryFeature = new QueryFeature(queryCollection);
            features.Set<IQueryFeature>(queryFeature);
        }

        // Request stream logic
        if (body != null)
        {
            var jsonBody = JsonSerializer.Serialize(body);

            var requestFeature = new HttpRequestFeature();
            requestFeature.Headers.ContentType = "application/json";
            requestFeature.Body = jsonBody.ToStream();
            features.Set<IHttpRequestFeature>(requestFeature);
        }

        var responseFeature = new HttpResponseFeature();
        features.Set<IHttpResponseFeature>(responseFeature);

        var context = new DefaultHttpContext(features);
        //var context = new DefaultHttpContext();

        if (routeValues != null)
        {
            context.Request.RouteValues = new RouteValueDictionary(routeValues);
        }

        endpoint.Middleware.Add(new HttpMiddleware(context));

        // Prepare a response stream for the test
        var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        // Initialize the endpoint and execute it
        await endpoint.InitializeAsync(new HttpApiEndpointContext(context));
        await endpoint.ExecuteAsync();

        // Reset the response stream
        responseStream.Position = 0;

        // Assume a result  TODO: make it respect empty results
        var jsonResult = await context.Response.Body.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<T>(jsonResult)!;

        return result;
    }

    public static Stream ToStream(this string s)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(s ?? ""));
    }

    public static async Task<byte[]> ReadAsByteArrayAsync(this Stream source)
    {
        // Optimization
        if (source is MemoryStream memorySource)
            return memorySource.ToArray();

        using var memoryStream = new MemoryStream();
        await source.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public static async Task<string> ReadAsStringAsync(this Stream stream)
    {
        return Encoding.UTF8.GetString(await ReadAsByteArrayAsync(stream));
    }
}
