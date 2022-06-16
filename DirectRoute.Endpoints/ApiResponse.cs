namespace DirectRoute.Endpoints;

/// <summary>
/// Server uses this to serialize in order to allow Result to be polymorphic
/// </summary>
public class ApiResponse : IApiResponse
{
    public object? Result { get; set; }
    public ApiResponseSpan? Span { get; set; }
}

/// <summary>
/// Clients should use this to deserialize in order to have a typed Result
/// </summary>
public class ApiResponse<T> : IApiResponse
{
    public T? Result { get; set; }
    public ApiResponseSpan? Span { get; set; }

    object? IApiResponse.Result => Result;

    public static implicit operator T?(ApiResponse<T> response)
    {
        return response.Result;
    }
}
