using System.Security.Claims;

namespace DirectRoute.Endpoints.Server;

/// <summary>
/// The context passed to the ApiEndpoint.InitializeAsync method which provides access to the
/// current user (implementation specific).
/// </summary>
public interface IApiEndpointContext
{
    ClaimsPrincipal CurrentUser { get; }
}