namespace DirectRoute.Endpoints;

public class ValidationFailure
{
    public string? Field { get; set; }
    public string? Message { get; set; }

    public ValidationFailure()
    {
    }

    public ValidationFailure(string message)
    {
        Message = message;
    }

    public ValidationFailure(string field, string message)
    {
        Field = field;
        Message = message;
    }

    public static implicit operator ValidationFailure(string value)
    {
        return new ValidationFailure(value);
    }
}