namespace DirectRoute.Endpoints.Server;

/// <summary>
/// Provides an extension point to customize the behavior of endpoints outside of the endpoint's
/// implementation.  For example, HttpMiddleware provides all the functionality neccessary to
/// translate the input and output of an endpoint from the HTTP request and response
/// respectively.
/// </summary>
public interface IApiEndpointMiddleware
{
    Task InitializeAsync(ApiEndpoint endpoint);
    Task LoadDataAsync(ApiEndpoint endpoint);
    Task<bool?> IsFound(ApiEndpoint endpoint);
    Task<bool?> IsOwner(ApiEndpoint endpoint);
    Task<bool?> IsAuthorized(ApiEndpoint endpoint, bool isOwner);
    Task HandleNotFoundAsync(ApiEndpoint endpoint);
    Task HandleUnauthorizedAsync(ApiEndpoint endpoint);
    Task HandleBadRequestAsync(ApiEndpoint endpoint, IReadOnlyList<ValidationFailure> failures);
    Task WriteResponseAsync(object? response);
}