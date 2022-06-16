namespace DirectRoute.Endpoints;

public class ApiResponseSpan : IApiResponseSpan
{
    public string Name { get; init; }
    public DateTime Started { get; } = DateTime.UtcNow;
    public DateTime? Ended { get; private set; }
    public TimeSpan Elapsed => Ended - Started ?? TimeSpan.Zero;
    public List<IApiResponseSpan> Subspans { get; } = new();

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

    public IApiResponseSpan Time(string name)
    {
        var subspan = new ApiResponseSpan(name);
        Subspans.Add(subspan);
        return subspan;
    }
}