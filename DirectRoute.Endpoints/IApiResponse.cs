namespace DirectRoute.Endpoints;

public interface IApiResponse
{
    object? Result { get; }
    ApiResponseSpan? Span { get; }
}
