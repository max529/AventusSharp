using AventusSharp.Data;
using AventusSharp.Tools.Attributes;
using CSharpToTypescript.Container;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToTypescript
{
    internal static class Tools
    {
        public static bool ExportToTypesript(INamedTypeSymbol type, bool defaultValue)
        {
            List<AttributeData> attrs = type.GetAttributes().ToList();
            if (defaultValue)
            {
                if (attrs.Find(p => p.AttributeClass != null && p.AttributeClass.ToString() == typeof(NoTypescript).FullName) != null)
                {
                    return false;
                }
            }
            else
            {
                if (attrs.Find(p => p.AttributeClass != null && p.AttributeClass.ToString() == typeof(Typescript).FullName) != null)
                {
                    return true;
                }
            }

            return defaultValue;
        }


        public static bool HasAttribute<X>(ISymbol type)
        {
            List<AttributeData> attrs = type.GetAttributes().ToList();
            if (attrs.Find(p => p.AttributeClass != null && p.AttributeClass.ToString() == typeof(X).FullName) != null)
            {
                return true;
            }
            return false;
        }

        public static bool IsSameType<X>(INamedTypeSymbol type)
        {
            return type.ToString() == typeof(X).FullName;
        }

        public static string GetRelativePath(string currentPathTxt, string importPathTxt)
        {
            List<string> currentPath = currentPathTxt.Split(Path.DirectorySeparatorChar).ToList();
            // start from the directory not the file
            currentPath.Remove(currentPath.Last());
            List<string> importPath = importPathTxt.Split(Path.DirectorySeparatorChar).ToList();

            for (int i = 0; i < currentPath.Count; i++)
            {
                if (importPath.Count > i)
                {
                    if (currentPath[i] == importPath[i])
                    {
                        currentPath.RemoveAt(i);
                        importPath.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            string finalPathToImport = "";
            for (int i = 0; i < currentPath.Count; i++)
            {
                finalPathToImport += "../";
            }
            if (finalPathToImport == "")
            {
                finalPathToImport += "./";
            }
            finalPathToImport += string.Join("/", importPath);
            return finalPathToImport;
        }

        public static string GetFullName(ISymbol type)
        {
            List<string> parentNames = new List<string>();
            ISymbol t = type;
            while (t.ContainingType != null)
            {
                parentNames.Add(t.ContainingType.Name);
                t = t.ContainingType;
            }
            if (parentNames.Count > 0)
            {
                return type.ContainingNamespace.ToString() + "." + string.Join("+", parentNames) + "+" + type.Name;
            }
            return type.ContainingNamespace.ToString() + "." + type.Name;
        }
        public static bool Is<X>(INamedTypeSymbol type, bool avoidInterface = false, bool avoidBaseAssembly = false)
        {
            bool result;
            if (!avoidInterface)
            {
                result = type.AllInterfaces.ToList().Find(p => IsSameType<X>(p)) != null || Tools.GetFullName(type) == typeof(X).FullName;
            }
            else
            {
                result = type.AllInterfaces.ToList().Find(p => IsSameType<X>(p)) != null;
            }
            if (!result)
            {
                return false;
            }
            if (avoidBaseAssembly)
            {
                string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
                if (type.IsGenericType)
                {
                    fullName += "`" + type.TypeParameters.Length;
                }
                Type? realType = ProjectManager.Config.compiledAssembly.GetType(fullName);
                if (realType != null && realType.Assembly == typeof(IStorable).Assembly)
                {
                    return false;
                }
            }
            return true;
        }
        public static Type? GetCompiledType(INamedTypeSymbol type)
        {
            string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (type.IsGenericType)
            {
                fullName += "`" + type.TypeParameters.Length;
            }
            fullName += ", " + type.ContainingAssembly.Name;
            Type? realType = Type.GetType(fullName);
            return realType;
        }

        public static INamedTypeSymbol? GetTypeSymbol(Type type)
        {
            ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(type.FullName ?? "");
            if (typeSymbol is INamedTypeSymbol namedType)
            {
                return namedType;
            }
            return null;
        }

        public static bool IsSubclass(Type parent, Type child)
        {
            while (child != null && child != typeof(object))
            {
                var cur = child.IsGenericType ? child.GetGenericTypeDefinition() : child;
                if (parent == cur)
                {
                    return true;
                }
                child = child.BaseType;
            }
            return false;
        }
    }
}
