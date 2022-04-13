namespace DirectRoute.Endpoints;

public class RouteModule
{
    public string Prefix { get; set; }

    private readonly RoutesBase routes;

    public RouteModule(RoutesBase routes, string prefix)
    {
        this.routes = routes;
        Prefix = prefix;
    }

    private string DerivePath(string path)
    {
        if (string.IsNullOrEmpty(Prefix))
            return path;
        else
            return $"{Prefix}/{path}";
    }

    public Route<T> Get<T>(string path) where T : IEndpoint
    {
        return routes.Get<T>(DerivePath(path));
    }

    public Route<T> Post<T>(string path) where T : IEndpoint
    {
        return routes.Post<T>(DerivePath(path));
    }

    public Route<T> Put<T>(string path) where T : IEndpoint
    {
        return routes.Put<T>(DerivePath(path));
    }

    public Route<T> Delete<T>(string path) where T : IEndpoint
    {
        return routes.Delete<T>(DerivePath(path));
    }
}