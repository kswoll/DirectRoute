using System.Text.Json;

namespace DirectRoute.Endpoints.Server
{
    public class DirectRouteServerConfiguration : DirectRouteConfiguration
    {
        public IReadOnlyList<Type> EndpointInterfaceTypes { get; }
        public IReadOnlyList<Type> EndpointImplementationTypes { get; }
        public IReadOnlyDictionary<Type, Type> EndpointImplemenationsByInterfaceType { get; }
        public IReadOnlyDictionary<Type, Type> EndpointInterfacesByImplementationType { get; }

        public DirectRouteServerConfiguration(
            IReadOnlyList<Type> endpointInterfaceTypes,
            IReadOnlyList<Type> endpointImplementationTypes,
            IReadOnlyDictionary<Type, Type> endpointImplemenationsByInterfaceType,
            IReadOnlyDictionary<Type, Type> endpointInterfacesByImplementionType,
            JsonSerializerOptions jsonOptions,
            QueryStringOptions? queryStringOptions = null
        )
            : base(jsonOptions, queryStringOptions)
        {
            EndpointInterfaceTypes = endpointInterfaceTypes;
            EndpointImplementationTypes = endpointImplementationTypes;
            EndpointImplemenationsByInterfaceType = endpointImplemenationsByInterfaceType;
            EndpointInterfacesByImplementationType = endpointInterfacesByImplementionType;
        }
    }
}