namespace DirectRoute.Endpoints.Server
{
    /// <summary>
    /// Internal because consumers of this library should have no need to inspect this information.
    /// Registered as a singletong in AddDirectRoute in DirectRouteExtensions.
    /// </summary>
    internal class DirectRouteConfiguration
    {
        public IReadOnlyList<Type> EndpointInterfaceTypes { get; }
        public IReadOnlyList<Type> EndpointImplementationTypes { get; }
        public IReadOnlyDictionary<Type, Type> EndpointImplemenationsByInterfaceType { get; }

        public DirectRouteConfiguration(IReadOnlyList<Type> endpointInterfaceTypes, IReadOnlyList<Type> endpointImplementationTypes,
            IReadOnlyDictionary<Type, Type> endpointImplemenationsByInterfaceType)
        {
            EndpointInterfaceTypes = endpointInterfaceTypes;
            EndpointImplementationTypes = endpointImplementationTypes;
            EndpointImplemenationsByInterfaceType = endpointImplemenationsByInterfaceType;
        }
    }
}