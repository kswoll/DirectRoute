using System.Reflection;

namespace DirectRoute.Endpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ReferenceAssembliesAttribute : Attribute
{
    public Assembly[] Assemblies { get; }

    public ReferenceAssembliesAttribute(Assembly[] assemblies)
    {
        Assemblies = assemblies;
    }
}
