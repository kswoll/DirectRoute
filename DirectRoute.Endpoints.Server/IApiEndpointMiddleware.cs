namespace DirectRoute.Endpoints.Server;

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