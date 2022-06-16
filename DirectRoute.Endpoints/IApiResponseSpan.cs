namespace DirectRoute.Endpoints;

public interface IApiResponseSpan : IDisposable
{
    string Name { get; }
    IApiResponseSpan Time(string name);
}