using AventusSharp.Tools.Attributes;
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


        public static Type? GetCompiledType(ISymbol type) 
        {
            string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
            //if (type.IsGenericType)
            //{
            //    fullName += "`" + type.TypeParameters.Length;
            //}
            return null;
        }
    }
}
