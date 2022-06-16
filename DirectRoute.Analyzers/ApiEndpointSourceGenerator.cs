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
            //Debugger.Launch();

            IncrementalValuesProvider<ApiSourceGeneratorContext> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)
                .Collect()
                .SelectMany((x, _) => x.Distinct());

            //IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses = context
            //    .CompilationProvider
            //    .Combine(classDeclarations.Collect());  Note: don't EVER use this technique or it will turn your generator into a non-incremental one.

            context.RegisterSourceOutput(classDeclarations, static (spc, source) => Execute(spc, source));

            static bool IsSyntaxTargetForGeneration(SyntaxNode node) => node is ClassDeclarationSyntax type && type.BaseList?.Types.Count > 0;

            static ApiSourceGeneratorContext GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
            {
                try
                {
                    return GetSemanticTargetForGenerationImpl(context);
                }
                catch (Exception ex)
                {
#if DEBUG
                    // When running with a Debug build, we launch the debugger so we can figure out what the problem is.
                    Debugger.Launch();
#endif
                    return new ApiSourceGeneratorContext(ex.ToString());
                }
            }

            static ApiSourceGeneratorContext GetSemanticTargetForGenerationImpl(GeneratorSyntaxContext context)
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
                        var apiContext = new ApiSourceGeneratorContext(classDeclaration, type, context.SemanticModel, iEndpointType, apiEndpointType);
                        if (!apiContext.IsInvalid && !apiContext.IsDefaultInvoke)
                            return apiContext;
                    }
                }

                return null;
            }
        }

        private static void Execute(SourceProductionContext context, ApiSourceGeneratorContext apiContext)
        {
            try
            {
                ExecuteImpl(context, apiContext);
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

        private static void ExecuteImpl(SourceProductionContext context, ApiSourceGeneratorContext apiContext)
        {
            // Work around situations where sometimes the compilation won't contain the class.  When this happened to me,
            // it was due to starting to type "partial" with the cursor at the beginning of the class name.
//            if (!compilation.ContainsSyntaxTree(classDeclaration.SyntaxTree))
//                continue;

            if (apiContext.InvalidSemanticTargetError != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointInterface, null, apiContext.InvalidSemanticTargetError));
            }

            var classDeclaration = apiContext.ClassDeclaration;
            if (apiContext.IsInvalidEndpointInterface)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointInterface, classDeclaration.GetLocation(),
                    "Endpoint interfaces must declare a single Invoke method"));
                return;
            }

            var declaringTypes = classDeclaration.GetContainingTypeDeclarations().Reverse().ToArray();
            var declaringTypeNames = declaringTypes.Select(x => x.Identifier.ToString()).ToArray();

            foreach (var missingProperty in apiContext.MissingProperties)
            {
                context.ReportDiagnostic(Diagnostic.Create(InvalidEndpointInterface, classDeclaration.GetLocation(),
                    $"Missing required property '{missingProperty}' defined by the interface's Invoke method"));
            }

            var returnType = apiContext.ReturnType;
            var parameters = string.Join(", ", apiContext.Parameters.Select(x => $"{x.Type} {x.Name}{x.DefaultValue}"));

            var typeParameters = classDeclaration.TypeParameterList == null || classDeclaration.TypeParameterList.Parameters.Count == 0
                ? ""
                : $"<{string.Join(", ", classDeclaration.TypeParameterList.Parameters.Select(x => x.Identifier.ValueText))}>";

            // Build up the source code
            string start =
$@"// Auto-generated code
using System;

namespace {apiContext.Namespace}
{{
{string.Join($"\r\n", declaringTypeNames.Select(x => $"    public partial class {x} {{"))}

public partial class {apiContext.Name}{typeParameters}
{{
    public async {returnType} Invoke({parameters})
    {{
";
            var properties = new StringBuilder();
            foreach (var parameter in apiContext.Parameters)
            {
                var propertyName = parameter.Name.Capitalize();
                var assignment = $"            {propertyName} = {parameter.Name};";
                properties.AppendLine(assignment);
            }

            string returnStatement = "";
            if (apiContext.HasReturnStatement)
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
            context.AddSource($"{apiContext.Name}.{typeParameters.Length}.api.cs", source);
        }
    }

    public class ApiSourceGeneratorContext : IEquatable<ApiSourceGeneratorContext>
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string FullClassName { get; set; }
        public ClassDeclarationSyntax ClassDeclaration { get; set; }
        public bool IsInvalid { get; set; }
        public bool IsInvalidEndpointInterface { get; set; }
        public bool IsDefaultInvoke { get; set; }
        public string InvalidSemanticTargetError { get; set; }
        public List<string> MissingProperties { get; set; } = new();
        public string ReturnType { get; set; }
        public List<(string Name, string Type, string DefaultValue)> Parameters { get; set; } = new();
        public bool HasReturnStatement { get; set; }

        public ApiSourceGeneratorContext(string invalidSemanticTargetError)
        {
            InvalidSemanticTargetError = invalidSemanticTargetError;
        }

        public ApiSourceGeneratorContext(ClassDeclarationSyntax classDeclaration, ITypeSymbol type, SemanticModel semanticModel, ITypeSymbol iEndpointType, ITypeSymbol apiEndpointType)
        {
            var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            Name = type.Name;
            Namespace = type.ContainingNamespace.ToDisplayString();
            FullClassName = type.ToDisplayString(symbolDisplayFormat);
            ClassDeclaration = classDeclaration;

            // Work around situations where sometimes the compilation won't contain the class.  When this happened to me,
            // it was due to starting to type "partial" with the cursor at the beginning of the class name.
//            if (!compilation.ContainsSyntaxTree(classDeclaration.SyntaxTree))
//                continue;

            var interfaceType = type.Interfaces.SingleOrDefault(x => x.Interfaces.Any(y => y.Equals(iEndpointType, SymbolEqualityComparer.Default)));
            if (interfaceType == null)
            {
                IsInvalid = true;
                return;  // TODO: consider making a warning instead
            }

            var invokeMethods = interfaceType.GetMembers("Invoke").ToArray();
            if (invokeMethods.Length != 1)
            {
                IsInvalid = true;
                IsInvalidEndpointInterface = true;
                return;
            }

            var invokeMethod = interfaceType.GetMembers("Invoke").SingleOrDefault() as IMethodSymbol;

            // If there are no parameters, the default `Invoke` method on ApiEndpoint will suffice.
            if (invokeMethod.Parameters.Length == 0)
            {
                IsDefaultInvoke = true;
                return;
            }

            var declaringTypes = classDeclaration.GetContainingTypeDeclarations().Reverse().ToArray();
            var declaringTypeNames = declaringTypes.Select(x => x.Identifier.ToString()).ToArray();

            var existingPropertiesByName = new Dictionary<string, IPropertySymbol>();
            foreach (var property in type.GetAllMembers().OfType<IPropertySymbol>())
            {
                if (!existingPropertiesByName.ContainsKey(property.Name))
                    existingPropertiesByName.Add(property.Name, property);
            }
            foreach (var parameter in invokeMethod.Parameters)
            {
                if (!existingPropertiesByName.ContainsKey(parameter.Name.Capitalize()))
                {
                    MissingProperties.Add(parameter.Name.Capitalize());
                }
            }

            ReturnType = invokeMethod.ReturnType.ToDisplayString();
            Parameters.AddRange(invokeMethod.Parameters.Select(x => (x.Name, x.Type.ToDisplayString(), GetDefaultValue(x))));

            var taskTypeT = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            if (invokeMethod.ReturnType.OriginalDefinition.IsEqual(taskTypeT))
                HasReturnStatement = true;
        }

        public bool Equals(ApiSourceGeneratorContext other)
        {
            if (FullClassName != other.FullClassName)
                return false;

            if (ReturnType != other.ReturnType)
                return false;

            if (Parameters.Count != other.Parameters.Count)
                return false;

            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                var otherParameter = other.Parameters[i];
                if (parameter.Name != otherParameter.Name || parameter.Type != otherParameter.Type || parameter.DefaultValue != otherParameter.DefaultValue)
                    return false;
            }

            return true;
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