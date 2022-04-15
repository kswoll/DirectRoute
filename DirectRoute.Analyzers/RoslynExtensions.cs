using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace DirectRoute.Analyzers
{
    public static class RoslynExtensions
    {
        public static ISymbol[] GetAllMembers(this ITypeSymbol type)
        {
            if (type.BaseType != null)
                return type.BaseType.GetAllMembers().Concat(type.GetMembers().ToArray()).ToArray();
            else
                return type.GetMembers().ToArray();
        }

        public static TypeDeclarationSyntax GetContainingTypeDeclaration(this SyntaxNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TypeDeclarationSyntax syntax)
                    return syntax;
                current = current.Parent;
            }
            return null;
        }

        public static IEnumerable<TypeDeclarationSyntax> GetContainingTypeDeclarations(this SyntaxNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is TypeDeclarationSyntax syntax)
                    yield return syntax;
                current = current.Parent;
            }
        }

        public static string Capitalize(this string s)
        {
            return string.Concat(s[0].ToString().ToUpper(), s.Substring(1));
        }

        public static bool IsSubclassOf(this ITypeSymbol type, ITypeSymbol baseType)
        {
            var current = type.BaseType;
            while (current != null)
            {
                if (current.Equals(baseType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
                current = current.BaseType;
            }

            return false;
        }

        public static bool IsEqual(this ITypeSymbol type, ITypeSymbol compareTo)
        {
            if (type == null)
                return false;
            return type.Equals(compareTo, SymbolEqualityComparer.Default);
        }

        public static ITypeSymbol GetGenericArgument(this ITypeSymbol type, ITypeSymbol unconstructedType, int argumentIndex)
        {
            var current = type;
            while (current != null)
            {
                if (current.OriginalDefinition.IsEqual(unconstructedType))
                {
                    return ((INamedTypeSymbol)current).TypeArguments[argumentIndex];
                }
                current = current.BaseType;
            }
            if (type is INamedTypeSymbol namedTypeSymbol)
            {
                foreach (var intf in namedTypeSymbol.AllInterfaces)
                {
                    if (intf.OriginalDefinition.IsEqual(unconstructedType))
                    {
                        return intf.TypeArguments[argumentIndex];
                    }
                }
            }
            return null;
        }
    }
}
