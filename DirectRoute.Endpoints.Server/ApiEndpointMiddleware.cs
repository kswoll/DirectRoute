namespace DirectRoute.Endpoints.Server;

/// <summary>
/// Convenience implementation of IApiEndpointMiddleware that implements that interface
/// and exposes the methods as virtual methods that can be overridden and also provides
/// both async and non async versions for you to override.
/// </summary>
public class ApiEndpointMiddleware : IApiEndpointMiddleware
{
    protected virtual void Initialize(ApiEndpoint endpoint)
    {
    }

    protected virtual Task InitializeAsync(ApiEndpoint endpoint)
    {
        return Task.CompletedTask;
    }

    protected virtual void LoadData(ApiEndpoint endpoint)
    {
    }

    protected virtual Task LoadDataAsync(ApiEndpoint endpoint)
    {
        return Task.CompletedTask;
    }

    protected virtual bool? IsAuthorized(ApiEndpoint endpoint, bool isOwner)
    {
        return null;
    }

    protected virtual Task<bool?> IsAuthorizedAsync(ApiEndpoint endpoint, bool isOwner)
    {
        return Task.FromResult<bool?>(null);
    }

    protected virtual bool? IsFound(ApiEndpoint endpoint)
    {
        return null;
    }

    protected virtual Task<bool?> IsFoundAsync(ApiEndpoint endpoint)
    {
        return Task.FromResult<bool?>(null);
    }

    protected virtual bool? IsOwner(ApiEndpoint endpoint)
    {
        return null;
    }

    protected virtual Task<bool?> IsOwnerAsync(ApiEndpoint endpoint)
    {
        return Task.FromResult<bool?>(null);
    }

    protected virtual void HandleNotFound(ApiEndpoint endpoint)
    {
    }

    protected virtual Task HandleNotFoundAsync(ApiEndpoint endpoint)
    {
        return Task.CompletedTask;
    }

    protected virtual void HandleUnauthorized(ApiEndpoint endpoint)
    {
    }

    protected virtual Task HandleUnauthorizedAsync(ApiEndpoint endpoint)
    {
        return Task.CompletedTask;
    }

    protected virtual void HandleBadRequest(ApiEndpoint endpoint, IReadOnlyList<ValidationFailure> failures)
    {
    }

    protected virtual Task HandleBadRequestAsync(ApiEndpoint endpoint, IReadOnlyList<ValidationFailure> failures)
    {
        return Task.CompletedTask;
    }

    protected virtual void WriteResponse(object? response)
    {
    }

    protected virtual Task WriteResponseAsync(object? response)
    {
        return Task.CompletedTask;
    }

    async Task IApiEndpointMiddleware.InitializeAsync(ApiEndpoint endpoint)
    {
        Initialize(endpoint);
        await InitializeAsync(endpoint);
    }

    async Task IApiEndpointMiddleware.LoadDataAsync(ApiEndpoint endpoint)
    {
        LoadData(endpoint);
        await LoadDataAsync(endpoint);
    }

    async Task<bool?> IApiEndpointMiddleware.IsAuthorized(ApiEndpoint endpoint, bool isOwner)
    {
        var result = IsAuthorized(endpoint, isOwner);
        if (result == null)
            result = await IsAuthorizedAsync(endpoint, isOwner);
        return result;
    }

    async Task<bool?> IApiEndpointMiddleware.IsFound(ApiEndpoint endpoint)
    {
        var result = IsFound(endpoint);
        if (result == null)
            result = await IsFoundAsync(endpoint);
        return result;
    }

    async Task<bool?> IApiEndpointMiddleware.IsOwner(ApiEndpoint endpoint)
    {
        var result = IsOwner(endpoint);
        if (result == null)
            result = await IsOwnerAsync(endpoint);
        return result;
    }

    async Task IApiEndpointMiddleware.HandleNotFoundAsync(ApiEndpoint endpoint)
    {
        HandleNotFound(endpoint);
        await HandleNotFoundAsync(endpoint);
    }

    async Task IApiEndpointMiddleware.HandleUnauthorizedAsync(ApiEndpoint endpoint)
    {
        HandleUnauthorized(endpoint);
        await HandleUnauthorizedAsync(endpoint);
    }

    async Task IApiEndpointMiddleware.HandleBadRequestAsync(ApiEndpoint endpoint, IReadOnlyList<ValidationFailure> failures)
    {
        HandleBadRequest(endpoint, failures);
        await HandleBadRequestAsync(endpoint, failures);
    }

    async Task IApiEndpointMiddleware.WriteResponseAsync(object? response)
    {
        WriteResponse(response);
        await WriteResponseAsync(response);
    }
}