using System.Net;

namespace DirectRoute.Blazor;

public interface IPageRouteHandler
{
    HttpStatusCode GetPageStatusCode(Type pageType, PageRoute route, RouteData routeData);
}
