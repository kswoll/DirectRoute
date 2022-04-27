using System.Security.Claims;

namespace DirectRoute.Endpoints.Server;

/// <summary>
/// This class typically handles HTTP requests through Web Api.  The behavior for that is defined in
/// HttpMiddleware, which is automatically added to the endpoint for HTTP requeests. However, it can
/// also be used by itself to take advantage of the method call to class pattern.
///
/// For HTTP:
/// Represents a single endpoint that can be invoked.  Furthermore, a new instance will be created
/// for every request, so you can declare fields and save transient state without trouble.  This
/// can be useful if you want to potentially return a 404 if an entity isn't found in the database.
/// For example, you can save the entity in a field for use in your OnExecute method while also using it
/// to return the 404 early if it ends up being null.
///
/// This class is by design not threadsafe, so you should avoid using a single instance with multiple
/// threads unless you perform thread locking yourself.
/// </summary>
public abstract class ApiEndpoint
{
    public IApiEndpointContext? Context { get; private set; }
    public List<IApiEndpointMiddleware> Middleware { get; private set; } = new List<IApiEndpointMiddleware>();

    public ClaimsPrincipal? User => Context?.CurrentUser;

    protected abstract Task OnExecuteAsync();

    public async Task InitializeAsync(IApiEndpointContext context)
    {
        Context = context;

        await OnInitializedAsync();

        foreach (var middleware in Middleware)
            await middleware.InitializeAsync(this);
    }

    protected virtual Task OnInitializedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to load data before validations (such as IsFound, IsAuthorized, and Validate) are run.
    /// For example, if your endpoint returns an entity but the entity wasn't in the database, you could
    /// use this method to load the entity and save it in a field and then your OnExecute implementation
    /// can use the field without loading the entity again.
    /// </summary>
    protected async Task LoadDataAsync()
    {
        foreach (var middleware in Middleware)
        {
            await middleware.LoadDataAsync(this);
        }

        OnLoadData();
        await OnLoadDataAsync();
    }

    protected virtual void OnLoadData()
    {
    }

    protected virtual Task OnLoadDataAsync()
    {
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync()
    {
        await LoadDataAsync();

        if (!await IsFoundAsync())
        {
            await HandleNotFoundAsync();
            return;
        }

        // If the user is unauthorized, handle it (by returning HTTP status Unauthorized) and return
        if (!await IsAuthorizedAsync())
        {
            await HandleUnauthorizedAsync();
            return;
        }

        var validations = new List<ValidationFailure>();
        await ValidateAsync(validations);

        if (validations.Any())
        {
            await HandleBadRequestAsync(validations);
            return;
        }

        await CallOnExecuteAsync();
    }

    /// <summary>
    /// Bypasses the API negotiation such as writing the response and checking permissions and invokes the endpoint
    /// directly.
    /// </summary>
    public async Task Invoke()
    {
        await LoadDataAsync();
        await OnExecuteAsync();
    }

    /// <summary>
    /// Should only ever call OnExecute and WriteResponse.  Exists so the generic version can handle the return value.
    /// </summary>
    protected virtual async Task CallOnExecuteAsync()
    {
        await OnExecuteAsync();
        await WriteResponseAsync();
    }

    protected virtual async Task WriteResponseAsync()
    {
        foreach (var middleware in Middleware)
        {
            await middleware.WriteResponseAsync(null);
        }
    }

    protected async Task<bool> IsFoundAsync()
    {
        foreach (var middleware in Middleware)
        {
            var isAuthorized = await middleware.IsFound(this);
            if (isAuthorized != null)
                return isAuthorized.Value;
        }

        if (!OnIsFound())
            return false;
        if (!await OnIsFoundAsync())
            return false;

        return true;
    }

    protected virtual bool OnIsFound()
    {
        return true;
    }

    protected virtual Task<bool> OnIsFoundAsync()
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Return true if the API should operate as the owner of the entities involved in the request.
    /// </summary>
    /// <returns></returns>
    protected async Task<bool> IsOwnerAsync()
    {
        foreach (var middleware in Middleware)
        {
            var isAuthorized = await middleware.IsOwner(this);
            if (isAuthorized != null)
                return isAuthorized.Value;
        }

        if (!OnIsOwner())
            return false;
        if (!await OnIsOwnerAsync())
            return false;

        return true;
    }

    protected virtual bool OnIsOwner()
    {
        return true;
    }

    protected virtual Task<bool> OnIsOwnerAsync()
    {
        return Task.FromResult(true);
    }

    protected async Task<bool> IsAuthorizedAsync()
    {
        var isOwner = await IsOwnerAsync();

        foreach (var middleware in Middleware)
        {
            var isAuthorized = await middleware.IsAuthorized(this, isOwner);
            if (isAuthorized != null)
                return isAuthorized.Value;
        }

        if (!OnIsAuthorized(isOwner))
            return false;
        if (!await OnIsAuthorizedAsync(isOwner))
            return false;

        if (isOwner == true)
            return true;

        return isOwner;
    }

    protected virtual bool OnIsAuthorized(bool isOwner)
    {
        return true;
    }

    protected virtual Task<bool> OnIsAuthorizedAsync(bool isOwner)
    {
        return Task.FromResult(true);
    }

    protected virtual void Validate(List<ValidationFailure> validations)
    {
    }

    protected virtual Task ValidateAsync(List<ValidationFailure> validations)
    {
        Validate(validations);
        return Task.CompletedTask;
    }

    protected virtual async Task HandleNotFoundAsync()
    {
        foreach (var middleware in Middleware)
        {
            await middleware.HandleNotFoundAsync(this);
        }
    }

    protected virtual async Task HandleUnauthorizedAsync()
    {
        foreach (var middleware in Middleware)
        {
            await middleware.HandleUnauthorizedAsync(this);
        }
    }

    protected virtual async Task HandleBadRequestAsync(IReadOnlyList<ValidationFailure> failures)
    {
        foreach (var middleware in Middleware)
        {
            await middleware.HandleBadRequestAsync(this, failures);
        }
    }

}

/// <summary>
/// Generic version that supports specifying the return type of your endpoint.  Will also write the return value
/// as JSON to the output stream.
/// </summary>
/// <typeparam name="T">The type of the return value for your endpoint</typeparam>
public abstract class ApiEndpoint<T> : ApiEndpoint
{
    /// <summary>
    /// Bypasses the API negotiation such as writing the response and checking permissions and invokes the endpoint
    /// directly.  This is useful when you are using endpoints outside of the context of the web api.
    /// </summary>
    public new async Task<T> Invoke()
    {
        await LoadDataAsync();
        return await OnExecuteAsync();
    }

    protected override abstract Task<T> OnExecuteAsync();

    protected override async Task CallOnExecuteAsync()
    {
        T result = await OnExecuteAsync();
        await WriteResponseAsync(result);
    }

    protected virtual async Task WriteResponseAsync(T result)
    {
        foreach (var middleware in Middleware)
        {
            await middleware.WriteResponseAsync(result);
        }
    }
}