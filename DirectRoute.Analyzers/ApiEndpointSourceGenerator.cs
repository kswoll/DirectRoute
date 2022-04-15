using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DirectRoute.Analyzers
{
    /// <summary>
    /// Responsible for auto-implementing IEndpoint interfaces by mapping the Invoke method
    /// parameters to properties of the same name.
    /// </summary>
    [Generator]
    public class ApiEndpointSourceGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor InvalidEndpointInterface = new(
            id: "XOGEN0001",
            title: "Invalid Endpoint Interface",
            messageFormat: "Invalid endpoint interface: '{0}'",
            category: "DirectRouteGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses = context
                .CompilationProvider
                .Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));

            static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax type && type.BaseList?.Types.Count > 0;

            static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
            {
                var classDeclaration = (ClassDeclarationSyntax)context.Node;
                var type = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(classDeclaration);

                var iEndpointType = context.SemanticModel.Compilation.GetTypeByMetadataName("DirectRoute.Endpoints.IEndpoint");
                var apiEndpointType = context.SemanticModel.Compilation.GetTypeByMetadataName("DirectRoute.Endpoints.Server.ApiEndpoint");

                if (type == null)
                    return null;

                if (!type.IsSubclassOf(apiEndpointType))
                    return null;

                foreach (var interfaceType in type.Interfaces)
                {
                    if (interfaceType.Interfaces.Any(x => x.Equals(iEndpointType, SymbolEqualityComparer.Default)))
                    {
                        return classDeclaration;
                    }
                }

                return null;
            }
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            try
            {
                ExecuteImpl(compilation, classes, context);
            }
            catch (Exception ex)
            {
#if DEBUG
                // When running with a Debug build, we launch the debugger so we can figure out what the problem is.
                Debugger.Launch();
#endif
                context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointInterface, null, ex.ToString()));
            }
        }

        private static void ExecuteImpl(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            IEnumerable<ClassDeclarationSyntax> distinctClasses = classes.Distinct();

            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskTypeT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            var iEndpointType = compilation.GetTypeByMetadataName("DirectRoute.Endpoints.IEndpoint");

            foreach (var classDeclaration in distinctClasses)
            {
                // Work around situations where sometimes the compilation won't contain the class.  When this happened to me,
                // it was due to starting to type "partial" with the cursor at the beginning of the class name.
                if (!compilation.ContainsSyntaxTree(classDeclaration.SyntaxTree))
                    continue;

                var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var type = (ITypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration);
                var interfaceType = type.Interfaces.SingleOrDefault(x => x.Interfaces.Any(y => y.Equals(iEndpointType, SymbolEqualityComparer.Default)));
                if (interfaceType == null)
                    continue;  // TODO: consider making a warning instead

                var invokeMethods = interfaceType.GetMembers("Invoke").ToArray();
                if (invokeMethods.Length != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointInterface, classDeclaration.GetLocation(),
                        "Endpoint interfaces must declare a single Invoke method"));
                    return;
                }
                var invokeMethod = interfaceType.GetMembers("Invoke").SingleOrDefault() as IMethodSymbol;

                // If there are no parameters, the default `Invoke` method on ApiEndpoint will suffice.
                if (invokeMethod.Parameters.Length == 0)
                    continue;

                var declaringTypes = classDeclaration.GetContainingTypeDeclarations().Reverse().ToArray();
                var declaringTypeNames = declaringTypes.Select(x => x.Identifier.ToString()).ToArray();

                var existingPropertiesByName = type.GetAllMembers().OfType<IPropertySymbol>().ToDictionary(x => x.Name);
                foreach (var parameter in invokeMethod.Parameters)
                {
                    if (!existingPropertiesByName.ContainsKey(parameter.Name.Capitalize()))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointInterface, classDeclaration.GetLocation(),
                            $"Missing required property '{parameter.Name.Capitalize()}' of type '{parameter.Type.ToDisplayString()}' defined by the interface's Invoke method"));
                    }
                }

                var returnType = invokeMethod.ReturnType.ToDisplayString();
                var parameters = string.Join(", ", invokeMethod.Parameters.Select(x => $"{x.Type.ToDisplayString()} {x.Name}{GetDefaultValue(x)}"));

                var typeParameters = classDeclaration.TypeParameterList == null || classDeclaration.TypeParameterList.Parameters.Count == 0
                    ? ""
                    : $"<{string.Join(", ", classDeclaration.TypeParameterList.Parameters.Select(x => x.Identifier.ValueText))}>";

                // Build up the source code
                string start =
$@"// Auto-generated code
using System;

namespace {type.ContainingNamespace.ToDisplayString()}
{{
{string.Join($"\r\n", declaringTypeNames.Select(x => $"    public partial class {x} {{"))}

    public partial class {type.Name}{typeParameters}
    {{
        public async {returnType} Invoke({parameters})
        {{
";
                var properties = new StringBuilder();
                foreach (var parameter in invokeMethod.Parameters)
                {
                    var propertyName = parameter.Name.Capitalize();
                    var assignment = $"            {propertyName} = {parameter.Name};";
                    properties.AppendLine(assignment);
                }

                string returnStatement = "";
                if (invokeMethod.ReturnType.OriginalDefinition.IsEqual(taskTypeT))
                    returnStatement = "return ";

var end =
$@"
            {returnStatement}await Invoke();
        }}
    }}

{string.Join($"\r\n", declaringTypeNames.Select(x => $"    }}"))}
}}
";

                // Add the source code to the compilation
                var source = string.Concat(start, properties, end);
                context.AddSource($"{type.Name}.api.cs", source);

            }
        }

        private static string GetDefaultValue(IParameterSymbol parameter)
        {
            if (!parameter.HasExplicitDefaultValue)
                return "";

            var defaultValue = parameter.ExplicitDefaultValue;
            if (defaultValue is bool)
                defaultValue = defaultValue.ToString().ToLower();
            if (defaultValue == null)
                defaultValue = "default";

            return $" = {defaultValue}";
        }
    }
}