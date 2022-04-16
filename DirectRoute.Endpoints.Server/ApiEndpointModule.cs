namespace DirectRoute.Endpoints.Server;

public class ApiEndpointModule
{
    public IEndpointRouteBuilder Endpoints { get; }
    public string Prefix { get; }
    public List<IApiEndpointMiddleware> Middleware { get; } = new List<IApiEndpointMiddleware>();

    public ApiEndpointModule(IEndpointRouteBuilder endpoints, string prefix)
    {
        Endpoints = endpoints;
        Prefix = prefix.TrimStart('/').TrimEnd('/');
    }

    public IEndpointConventionBuilder MapGet<T>(string pattern = "")
        where T : ApiEndpoint
    {
        var result = Endpoints.MapGet(ConstructPattern(pattern), new ModuleEndpointRequestHandler<T>(this).HandleRequest());
        return result;
    }

    public IEndpointConventionBuilder MapPost<T>(string pattern = "")
        where T : ApiEndpoint
    {
        var result = Endpoints.MapPost(ConstructPattern(pattern), new ModuleEndpointRequestHandler<T>(this).HandleRequest());
        return result;
    }

    public IEndpointConventionBuilder MapDelete<T>(string pattern = "")
        where T : ApiEndpoint
    {
        var result = Endpoints.MapDelete(ConstructPattern(pattern), new ModuleEndpointRequestHandler<T>(this).HandleRequest());
        return result;
    }

    public IEndpointConventionBuilder MapPut<T>(string pattern = "")
        where T : ApiEndpoint
    {
        var result = Endpoints.MapPut(ConstructPattern(pattern), new ModuleEndpointRequestHandler<T>(this).HandleRequest());
        return result;
    }

    private string ConstructPattern(string pattern)
    {
        pattern = pattern.TrimStart('/');

        if (string.IsNullOrEmpty(pattern))
            return Prefix;
        else if (string.IsNullOrEmpty(Prefix))
            return pattern;
        else
            return Prefix + "/" + pattern;
    }

    private class ModuleEndpointRequestHandler<T>
        where T : ApiEndpoint
    {
        private readonly ApiEndpointModule module;

        public ModuleEndpointRequestHandler(ApiEndpointModule module)
        {
            this.module = module;
        }

        public RequestDelegate HandleRequest()
        {
            return module.Endpoints.GetRequestDelegate<T>(endpoint =>
            {
                foreach (var middleware in module.Middleware)
                {
                    endpoint.Middleware.Add(middleware);
                }
            });
        }
    }
}