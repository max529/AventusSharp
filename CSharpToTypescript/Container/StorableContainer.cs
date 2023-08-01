using AventusSharp.Data;
using AventusSharp.Tools.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpToTypescript.Container
{
    internal class StorableContainer : BaseContainer
    {

        public static bool Is(INamedTypeSymbol type, string fileName, out BaseContainer? result)
        {
            result = null;
            if (type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IStorable>(p)) != null)
            {
                if (Tools.ExportToTypesript(type, true))
                {
                    result = new StorableContainer(type);
                }
                return true;
            }
            return false;
        }

        private bool isInterface;
        private bool foundIStorable = false;
        public StorableContainer(INamedTypeSymbol type) : base(type)
        {
            isInterface = type.TypeKind == TypeKind.Interface;
        }

        protected override string WriteAction()
        {
            List<string> result = new List<string>();
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                AddIndent();
            }
            string documentation = GetDocumentation(type);
            if (documentation.Length > 0)
            {
                result.Add(documentation);
            }
            AddTxtOpen(GetAccessibilityExport(type) + GetAbstract() + GetKind() + GetTypeName(type, 0, true) + " " + GetExtension() + "{", result);
            result.Add(GetContent());
            AddTxtClose("}", result);

            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                RemoveIndent();
            }

            return string.Join("\r\n", result);
        }


        private string GetKind()
        {
            return isInterface ? "interface " : "class ";
        }
        private string GetAbstract()
        {
            if (!isInterface && type.IsAbstract)
            {
                return "abstract ";
            }
            return "";
        }

        private string GetExtension()
        {
            List<string> extends = new List<string>();
            List<string> implements = new List<string>();
            if (isInterface)
            {
                foreach (INamedTypeSymbol @interface in type.Interfaces)
                {
                    extends.Add(GetTypeName(@interface));
                }
            }
            else
            {
                foreach (INamedTypeSymbol @interface in type.Interfaces)
                {
                    implements.Add(GetTypeName(@interface));
                }
                if (type.BaseType != null)
                {
                    extends.Add(GetTypeName(type.BaseType));
                }
            }

            if (!foundIStorable)
            {
                if (isInterface)
                {
                    extends.Add("Aventus.IData");
                }
                else
                {
                    implements.Add("Aventus.IData");
                }
            }

            string txt = "";
            if (extends.Count > 0)
            {
                txt += "extends " + string.Join(", ", extends);
            }
            if (txt.Length > 0)
            {
                txt += " ";
            }

            if (implements.Count > 0)
            {
                txt += "implements " + string.Join(", ", implements);
            }
            if (txt.Length > 0)
            {
                txt += " ";
            }
            return txt;
        }

        public override string GetTypeName(ISymbol type, int depth = 0, bool genericExtendsConstraint = false)
        {
            string result = base.GetTypeName(type, depth, genericExtendsConstraint);
            if (result == "Aventus.IData")
            {
                foundIStorable = true;
            }
            return result;
        }
        

        private string GetContent()
        {
            IEnumerable<ISymbol> members = type.GetMembers();
            List<string> result = new List<string>();
            if (!isInterface && !type.IsAbstract)
            {
                string typeName = "\"" + type.ContainingNamespace.ToString() + "." + type.Name + ", " + type.ContainingAssembly.Name + "\"";
                AddTxt("public static override get Fullname(): string { return " + typeName + "; }", result);
            }

            foreach (ISymbol member in members)
            {
                if (member.IsImplicitlyDeclared)
                {
                    continue;
                }
                if (member is IPropertySymbol propertySymbol)
                {
                    if (HasAttribute<NoTypescript>(member))
                    {
                        continue;
                    }

                    string documentation = GetDocumentation(member);
                    if (documentation.Length > 0)
                    {
                        result.Add(documentation);
                    }

                    string type = GetTypeName(propertySymbol.Type);
                    if (type.EndsWith("?"))
                    {
                        type = type.Substring(0, type.Length - 1);
                        type += " | null";
                    }

                    string txt = GetAccessibility(member) + member.Name + ": " + type + " = " + GetDefaultValue(member) + ";";
                    AddTxt(txt, result);
                }
                else if (member is IFieldSymbol fieldSymbol)
                {
                    string txt = GetAccessibility(member) + member.Name + ": " + GetTypeName(fieldSymbol.Type) + ";";
                    AddTxt(txt, result);
                }
            }

            return string.Join("\r\n", result);
        }

        private string GetDefaultValue(ISymbol symbol)
        {
            string result = "null";
            var equalsSyntax = symbol.DeclaringSyntaxReferences[0].GetSyntax() switch
            {
                PropertyDeclarationSyntax property => property.Initializer,
                VariableDeclaratorSyntax variable => variable.Initializer,
                _ => null
            };
            if (equalsSyntax is not null)
            {
                return equalsSyntax.Value.ToString();
            }
            return result;
        }
    }
}
