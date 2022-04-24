using System.Reflection;

namespace DirectRoute.Endpoints.Server;

public static class DirectRouteExtensions
{
    public static void AddDirectRoute(this IServiceCollection services, Type routesType, Assembly interfaceAssembly, Assembly implementationAssembly)
    {
        services.AddDirectRoute(routesType, new[] { interfaceAssembly }, new[] { implementationAssembly });
    }

    public static void AddDirectRoute(this IServiceCollection services, Type routesType, IReadOnlyList<Assembly> interfaceAssemblies, IReadOnlyList<Assembly> implementationAssemblies)
    {
        services.AddSingleton<EndpointInitializersBase>();
        services.AddSingleton<ApiEndpointRequestHandler>();
        services.AddSingleton<IApiEndpointProvider, DefaultApiEndpointProvider>();
        services.AddSingleton(typeof(RoutesBase), routesType);

        var configuration = CreateConfiguration(interfaceAssemblies, implementationAssemblies);
        services.AddSingleton(configuration);

        foreach (var endpointType in configuration.EndpointInterfaceTypes)
        {
            if (configuration.EndpointImplemenationsByInterfaceType.TryGetValue(endpointType, out var endpointImplementation))
            {
                // In order to bind open generic types (of the form MyClass<>) both the interface type AND the implementation type
                // must have the same arity (or number of generic parameters)
                var isValidGenericPair = endpointType.IsGenericTypeDefinition && endpointImplementation.IsGenericTypeDefinition && endpointType.GetGenericArguments().Length == endpointImplementation.GetGenericArguments().Length;

                if (!endpointType.IsGenericTypeDefinition || isValidGenericPair)
                {
                    // Allow injection of the endpoint by requesting just its interface.
                    services.AddScoped(endpointType, endpointImplementation);
                }
            }
        }

        // Also handle requests for the concrete implementation. Caution: if an implementation
        // doesn't expose an endpoint interface, putting this logic here will still capture it
        // (vs. putting it into the above loop)
        foreach (var implementation in configuration.EndpointImplementationTypes)
        {
            services.AddScoped(implementation);
        }
    }

    public static DirectRouteConfiguration CreateConfiguration(IReadOnlyList<Assembly> interfaceAssemblies, IReadOnlyList<Assembly> implementationAssemblies)
    {
        var endpointInterfaceTypes = interfaceAssemblies
            .SelectMany(x => x.GetTypes())
            .Where(x => x.GetInterfaces().Any(y => y == typeof(IEndpoint)))
            .ToArray();
        var endpointImplementationTypes = implementationAssemblies
            .SelectMany(x => x.GetTypes())
            .Where(x => typeof(ApiEndpoint).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .ToArray();

        // Aligns the interfaces and implementations together in a single array, removing any implementations
        // that don't implement an endpoint interface
        var endpointInterfacesAndImplementations = endpointImplementationTypes
            .Select(x => new
            {
                InterfaceType = x.GetInterfaces().SingleOrDefault(y => y != typeof(IEndpoint) && typeof(IEndpoint).IsAssignableFrom(y) && !y.IsAssignableFrom(x.BaseType)),
                ImplementationType = x
            })
            .Where(x => x.InterfaceType != null)
            .Select(x => new { InterfaceType = x.InterfaceType!, x.ImplementationType })  // Clear nullability
            .ToArray();

        // Scan for situations where multiple implementations implement the same interface.  That's invalid, and catching
        // it early here allows for highly descriptive exception messages.
        var endpointImplementationsByInterfaceType = new Dictionary<Type, Type>();
        var endpointInterfacesByImplementationType = new Dictionary<Type, Type>();
        foreach (var item in endpointInterfacesAndImplementations)
        {
            if (item.ImplementationType.BaseType != null && item.InterfaceType.IsAssignableFrom(item.ImplementationType.BaseType))
            {
                // If the endpoint interface is already defined in the base type, then this is a subclass and we want to skip it.
                continue;
            }

            var interfaceType = item.InterfaceType.IsGenericType ? item.InterfaceType.GetGenericTypeDefinition() : item.InterfaceType;
            if (!endpointImplementationsByInterfaceType.TryAdd(interfaceType, item.ImplementationType))
            {
                var alreadyRegisteredItem = endpointImplementationsByInterfaceType[interfaceType];
                throw new InvalidOperationException($"Interface type '{interfaceType.FullName}' for implementation '{item.ImplementationType.FullName}' has already been registered for '{alreadyRegisteredItem.FullName}'");
            }
            else
            {
                if (!endpointInterfacesByImplementationType.TryAdd(item.ImplementationType, interfaceType))
                {
                    var alreadyRegisteredItem = endpointInterfacesByImplementationType[item.ImplementationType];
                    throw new InvalidOperationException($"Interface type '{interfaceType.FullName}' for implementation '{item.ImplementationType.FullName}' has already been registered for '{alreadyRegisteredItem.FullName}'");
                }
            }
        }

        return new DirectRouteConfiguration(endpointInterfaceTypes, endpointImplementationTypes, endpointImplementationsByInterfaceType,
            endpointInterfacesByImplementationType);
    }

    public static void MapDirectRoute(this IEndpointRouteBuilder endpoints, RoutesBase routes)
    {
        // Register endpoints based on routes
        foreach (var route in routes.List)
        {
            // Register the endpoint and obtain the implementation
            var endpointImplementation = endpoints.MapRoute(route);

            var propertyNames = endpointImplementation.GetProperties().Select(x => x.Name).ToHashSet();
            var variableNames = route.Variables.Select(x => x.Variable!.Capitalize()).ToArray();
            var missingPropertyNames = variableNames.Where(x => !propertyNames.Contains(x)).ToArray();
            if (missingPropertyNames.Any())
                throw new InvalidOperationException($"Route {route.Path} expected the following missing properties on {endpointImplementation.Name}: {string.Join(", ", missingPropertyNames)}");
        }
    }
}
