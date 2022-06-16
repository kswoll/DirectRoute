using System.Net;

namespace DirectRoute.Blazor;

public interface IPageRouteHandler
{
    Task<HttpStatusCode> GetPageStatusCode(Type pageType, PageRoute route, RouteData routeData);
}
