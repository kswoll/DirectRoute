using System.Text.Json;

namespace DirectRoute;

public class DirectRouteConfiguration
{
    public JsonSerializerOptions JsonOptions { get; }
    public QueryStringOptions QueryStringOptions { get; }

    public DirectRouteConfiguration(JsonSerializerOptions jsonOptions, QueryStringOptions? queryStringOptions = null)
    {
        JsonOptions = jsonOptions;
        QueryStringOptions = queryStringOptions ?? new QueryStringOptions();
    }
}
