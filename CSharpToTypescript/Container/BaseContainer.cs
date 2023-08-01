using AventusSharp.Data;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpToTypescript.Container
{
    public enum IsContainerResult
    {
        TrueAndExport,
        True,
        False
    }
    internal abstract class BaseContainer
    {
        public List<ISymbol> unresolved = new List<ISymbol>();
        public string Namespace { get; private set; } = "";
        public INamedTypeSymbol type;
        public string Content { get; private set; } = "";
        public BaseContainer(INamedTypeSymbol type)
        {
            this.type = type;
            var splitted = type.ContainingNamespace.ToString()?.Split(".").ToList();
            if (splitted != null)
            {
                splitted.RemoveAt(0);
                Namespace = string.Join(".", splitted);
            }
        }

        public void Write()
        {
            Content = WriteAction();
        }
        protected abstract string WriteAction();

        protected int indent = 0;
        public string GetIndentedText(string txt)
        {
            for (int i = 0; i < indent; i++)
            {
                txt = "\t" + txt;
            }
            return txt;
        }
        public void AddTxt(string txt, List<string> result)
        {
            for (int i = 0; i < indent; i++)
            {
                txt = "\t" + txt;
            }
            result.Add(txt);
        }
        public void AddTxt(List<string> txts, List<string> result)
        {
            foreach (var txt in txts)
            {
                AddTxt(txt, result);
            }
        }
        public void AddTxtOpen(string txt, List<string> result)
        {
            AddTxt(txt, result);
            indent++;
        }
        public void AddTxtClose(string txt, List<string> result)
        {
            indent--;
            AddTxt(txt, result);
        }

        public void AddIndent()
        {
            indent++;
        }
        public void RemoveIndent()
        {
            indent--;
        }

        public static string GetAccessibilityExport(ISymbol type)
        {
            return type.DeclaredAccessibility switch
            {
                Accessibility.Public => "export ",
                Accessibility.Protected => "export ",
                Accessibility.Friend => "export ",
                Accessibility.ProtectedAndFriend => "export ",
                Accessibility.ProtectedOrFriend => "export ",
                _ => ""
            };
        }
        public static string GetAccessibility(ISymbol type)
        {
            return type.DeclaredAccessibility switch
            {
                Accessibility.Public => "public ",
                Accessibility.Protected => "protected ",
                Accessibility.Private => "private ",
                Accessibility.Friend => "protected ",
                Accessibility.ProtectedAndFriend => "protected ",
                Accessibility.ProtectedOrFriend => "protected ",
                _ => ""
            };
        }
        protected string GetDocumentation(ISymbol type)
        {
            List<string> result = new List<string>();
            string commentTxt = type.GetDocumentationCommentXml() ?? "";
            Match match = Regex.Match(commentTxt, @"<summary>([\s|\S]*)</summary>");
            if (match.Success)
            {
                string[] comments = match.Groups[1].Value.Trim().Split("\r\n");
                AddTxt("/**", result);
                foreach (string comment in comments)
                {
                    AddTxt(" * " + comment.Trim(), result);
                }

                AddTxt(" */", result);
            }
            return string.Join("\r\n", result);
        }
        public virtual string GetTypeName(ISymbol type, int depth = 0, bool genericExtendsConstraint = false)
        {
            string name = "";
            if (type.ContainingAssembly.Name == ProjectManager.CurrentAssemblyName)
            {
                SyntaxTree? general = this.type.Locations[0].SourceTree;
                SyntaxTree? current = type.Locations[0].SourceTree;
                if (general != current)
                {
                    // need to load file
                    if (!unresolved.Contains(type))
                    {
                        unresolved.Add(type);
                    }
                }

                name = type.Name;
            }
            else
            {
                name = type.ToString() ?? "";
            }
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                name = name.Split("<")[0];
                name = DetermineGenericType(namedType, name, depth, genericExtendsConstraint);
            }
            name = Replacer(name);

            return name;
        }
        protected string DetermineGenericType(INamedTypeSymbol type, string name, int depth, bool genericExtendsConstraint)
        {
            int i = 0;
            if(type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IStorable>(p)) != null)
            {
                i = 1;
            }
            List<string> gens = new List<string>();
            for (; i < type.TypeArguments.Length; i++)
            {
                ITypeSymbol genericType = type.TypeArguments[i];
                gens.Add(ParseGenericType(genericType, depth, genericExtendsConstraint));
            }
            if (gens.Count > 0)
            {
                name += "<" + string.Join(", ", gens) + ">";
            }
            return name;
        }

        protected virtual string ParseGenericType(ITypeSymbol type, int depth, bool genericExtendsConstraint)
        {
            if (type is INamedTypeSymbol casted)
            {
                return GetTypeName(casted, depth + 1);
            }
            if (type is ITypeParameterSymbol typeParameter)
            {
                if (genericExtendsConstraint)
                {
                    List<string> extends = new List<string>();
                    foreach (ITypeSymbol genericConstraint in typeParameter.ConstraintTypes)
                    {
                        string extendsTemp = GetTypeName(genericConstraint, depth + 1);
                        if (extendsTemp != "")
                        {
                            extends.Add(extendsTemp);
                        }
                    }
                    if (extends.Count > 0)
                    {
                        return type.Name + " extends " + string.Join(", ", extends);
                    }
                }
            }

            return type.Name;
        }


        protected bool HasAttribute<X>(ISymbol type)
        {
            return type.GetAttributes().ToList()
                .Find(p => p.AttributeClass != null && p.AttributeClass.ToString() == typeof(X).FullName) != null;
        }

        public string Replacer(string input)
        {
            input = input.Replace("int", "number");
            input = input.Replace("double", "number");
            input = input.Replace("float", "number");
            input = input.Replace("decimal", "number");
            input = input.Replace("bool", "boolean");
            input = input.Replace("String", "string");
            input = input.Replace("Boolean", "boolean");
            input = input.Replace("AventusSharp.Data.IStorable", "Aventus.IData");
            input = new Regex("AventusSharp\\.Data\\.Storable<.*?>").Replace(input, "Aventus.Data");
            input = new Regex("AventusSharp\\.Data\\.ResultWithError<(.*?)>").Replace(input, "Aventus.GenericResultWithError<$1>");
            input = new Regex("System\\.Collections\\.Generic\\.List<(.*?)>").Replace(input, "$1[]");


            return input;
        }
    }
}
