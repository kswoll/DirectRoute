using System.Security.Claims;

namespace DirectRoute.Endpoints.Server;

public interface IApiEndpointContext
{
    ClaimsPrincipal CurrentUser { get; }
}