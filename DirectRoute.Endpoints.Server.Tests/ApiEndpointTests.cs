using NUnit.Framework;
using Shouldly;

namespace DirectRoute.Endpoints.Server.Tests;

[TestFixture]
public partial class ApiEndpointTests
{
    [Test]
    public async Task BodyAttributeInEndpointInterfaceHonoredWhenBinding()
    {
        var endpoint = new BodyEndpoint();

        var result = await endpoint.Call<string>(body: "test", query: null, routeValues: null);
        result.ShouldBe("test");
    }

    [Test]
    public async Task QueryArgument()
    {
        var endpoint = new BodyEndpoint();

        var result = await endpoint.Call<string>(body: null, query: new { StringValue = "test" }, routeValues: null);
        result.ShouldBe("test");
    }

    [Test]
    public async Task RouteArgument()
    {
        var endpoint = new RouteEndpoint();

        var result = await endpoint.Call<string>(body: null, query: null, routeValues: new { StringValue = "test" });
        result.ShouldBe("test");
    }

    public partial class BodyEndpoint : ApiEndpoint<string>, IBodyEndpoint
    {
        public string StringValue { get; set; } = default!;

        protected override Task<string> OnExecuteAsync()
        {
            return Task.FromResult(StringValue);
        }
    }

    public interface IBodyEndpoint : IEndpoint
    {
        Task<string> Invoke([Body] string stringValue);
    }

    public partial class QueryEndpoint : ApiEndpoint<string>, IQueryEndpoint
    {
        public string StringValue { get; set; } = default!;

        protected override Task<string> OnExecuteAsync()
        {
            return Task.FromResult(StringValue);
        }
    }

    public interface IQueryEndpoint : IEndpoint
    {
        Task<string> Invoke(string stringValue);
    }

    public partial class RouteEndpoint : ApiEndpoint<string>, IRouteEndpoint
    {
        public string StringValue { get; set; } = default!;

        protected override Task<string> OnExecuteAsync()
        {
            return Task.FromResult(StringValue);
        }
    }

    public interface IRouteEndpoint : IEndpoint
    {
        Task<string> Invoke(string stringValue);
    }
}