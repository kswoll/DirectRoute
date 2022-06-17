namespace DirectRoute.Endpoints;

public class ApiResponseSpan : IDisposable
{
    public string Name { get; init; }
    public DateTime Started { get; init; } = DateTime.UtcNow;
    public DateTime? Ended { get; set; }
    public TimeSpan Elapsed => Ended - Started ?? TimeSpan.Zero;
    public List<ApiResponseSpan> Subspans { get; init; } = new();

    public ApiResponseSpan()
    {
        Name = "(anonymous span)";
    }

    public ApiResponseSpan(string? name)
    {
        Name = name ?? "";
    }

    private void End()
    {
        Ended = DateTime.UtcNow;
    }

    void IDisposable.Dispose()
    {
        End();
    }

    public ApiResponseSpan Time(string name)
    {
        var subspan = new ApiResponseSpan(name);
        Subspans.Add(subspan);
        return subspan;
    }
}