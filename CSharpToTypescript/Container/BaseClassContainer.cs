﻿using AventusSharp.Tools.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToTypescript.Container
{
    public abstract class BaseClassContainer : BaseContainer
    {
        private static List<INamedTypeSymbol> alreadyLoaded = new();

        private List<Type> matchingTypes = new();
        protected bool isInterface;

        protected BaseClassContainer(INamedTypeSymbol type) : base(type)
        {
            isInterface = type.TypeKind == TypeKind.Interface;
            
           

            LoadGenericsType();
            if (CanBeAdded)
            {
                GetGenericValues();
            }

            if (CanConvert())
            {
                IsConvertible = !isInterface && !type.IsAbstract;
            }
        }

        private void LoadGenericsType()
        {
            Type[] types = ProjectManager.Config.compiledAssembly.GetTypes();
            matchingTypes = new();
            string fullNameBase = Tools.GetFullName(type);
            foreach (Type t in types)
            {
                if (t.FullName != null && (t.FullName.StartsWith(fullNameBase + "`") || t.FullName == fullNameBase))
                {
                    matchingTypes.Add(t);

                }
            }
            matchingTypes.Sort((a, b) =>
            {
                int nbA = a.IsGenericType ? a.GetGenericArguments().Length : 0;
                int nbB = b.IsGenericType ? b.GetGenericArguments().Length : 0;

                return nbB - nbA;
            });

            INamedTypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(matchingTypes[0].FullName ?? "");
            if (typeSymbol != null)
            {
                type = typeSymbol;
            }
            if (alreadyLoaded.Contains(type))
            {
                CanBeAdded = false;
            }
            else
            {
                alreadyLoaded.Add(type);
            }
        }

        private void GetGenericValues()
        {
            Type definedType = matchingTypes.Last();
            if (matchingTypes.Count > 1)
            {
                while (true)
                {
                    if (definedType.BaseType == null)
                    {
                        break;
                    }

                    Type typeToCompare = definedType.IsGenericType ? definedType.GetGenericTypeDefinition() : definedType;

                    if (typeToCompare == matchingTypes[0])
                    {
                        break;
                    }

                    definedType = definedType.BaseType;
                }
            }
            if (definedType.IsGenericType)
            {
                Type[] definedParameters = definedType.GetGenericArguments();
                Type[] baseParameters = definedType.GetGenericTypeDefinition().GetGenericArguments();

                for (int i = 0; i < baseParameters.Length; i++)
                {
                    if (definedParameters[i].IsGenericTypeParameter)
                    {
                        defaultValueForGenerics.Add(baseParameters[i].Name, null);
                    }
                    else
                    {
                        ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(definedParameters[i].FullName ?? "");
                        if (typeSymbol != null)
                        {
                            defaultValueForGenerics.Add(baseParameters[i].Name, GetTypeName(typeSymbol, 0, false));
                        }
                    }
                }

            }

            defaultValueForGenerics = defaultValueForGenerics.OrderBy(p => p.Value).ToDictionary(p => p.Key, p => p.Value);
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
            if (IsConvertible)
            {
                AddTxt("@Convertible()", result);
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


        private string GetAbstract()
        {
            if (!isInterface && type.IsAbstract)
            {
                return "abstract ";
            }
            return "";
        }
        private string GetKind()
        {
            return isInterface ? "interface " : "class ";
        }

        private string GetExtension()
        {
            BaseContainer.enumToTypeof = true;
            List<string> extends = new List<string>();
            List<string> implements = new List<string>();
            List<string> extendsName = new();
            List<string> implementsName = new();
            string fullName = Tools.GetFullName(type);
            if (isInterface)
            {
                for (int i = matchingTypes.Count - 1; i >= 0; i--)
                {
                    ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(matchingTypes[i].FullName ?? "");
                    if (typeSymbol == null)
                    {
                        continue;
                    }
                    foreach (INamedTypeSymbol @interface in typeSymbol.Interfaces)
                    {
                        if (Tools.GetFullName(@interface) == fullName)
                        {
                            continue;
                        }
                        if (extendsName.Contains(@interface.Name))
                        {
                            continue;
                        }
                        if (IsValidExtendsInterface(@interface))
                        {
                            extendsName.Add(@interface.Name);
                            extends.Add(GetTypeName(@interface));
                        }
                    }
                }

            }
            else
            {
                for (int i = matchingTypes.Count - 1; i >= 0; i--)
                {
                    ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(matchingTypes[i].FullName ?? "");
                    if (typeSymbol == null)
                    {
                        continue;
                    }
                    foreach (INamedTypeSymbol @interface in typeSymbol.Interfaces)
                    {
                        if (Tools.GetFullName(@interface) == fullName)
                        {
                            continue;
                        }
                        if (extendsName.Contains(@interface.Name))
                        {
                            continue;
                        }
                        if (IsValidImplements(@interface))
                        {
                            implementsName.Add(@interface.Name);
                            implements.Add(GetTypeName(@interface));
                        }
                    }
                }

                if (type.BaseType != null && type.BaseType.Name != "Object" && Tools.GetFullName(type.BaseType) != fullName)
                {
                    if (IsValidExtendsClass(type.BaseType))
                    {
                        extends.Add(GetTypeName(type.BaseType));
                    }
                }
            }

            AddExtends(extends);
            AddImplements(implements);

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
            BaseContainer.enumToTypeof = false;
            return txt;
        }

        private string GetContent()
        {
            List<string> loadedFields = new List<string>();
            List<string> result = new List<string>();
            GetContentBefore(result);
            DefineFullname(result);

            for (int i = matchingTypes.Count - 1; i >= 0; i--)
            {
                Type type = matchingTypes[i].IsGenericType ? matchingTypes[i].GetGenericTypeDefinition() : matchingTypes[i];
                ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(type.FullName ?? "");
                if (typeSymbol != null)
                {
                    IEnumerable<ISymbol> members = typeSymbol.GetMembers();
                    foreach (ISymbol member in members)
                    {
                        if (member.IsImplicitlyDeclared)
                        {
                            continue;
                        }
                        if (member is IPropertySymbol propertySymbol)
                        {
                            if (loadedFields.Contains(member.Name))
                            {
                                continue;
                            }
                            if (HasAttribute<NoTypescript>(member))
                            {
                                continue;
                            }
                            if (!IsValidProperty(propertySymbol))
                            {
                                continue;
                            }
                            loadedFields.Add(member.Name);

                            string documentation = GetDocumentation(member);
                            if (documentation.Length > 0)
                            {
                                result.Add(documentation);
                            }

                            string memberName = member.Name;
                            string typeTxt = GetTypeName(propertySymbol.Type);
                            if (typeTxt.EndsWith("?"))
                            {
                                typeTxt = typeTxt.Substring(0, typeTxt.Length - 1);
                                memberName += "?";
                            }
                            string defaultValue = GetDefaultValue(propertySymbol, typeTxt);
                            bool isUndefined = false;
                            if(defaultValue == "undefined" && !memberName.EndsWith("?"))
                            {
                                isUndefined = true;
                                memberName += "!";
                            }
                            string txt = "";
                            if (isInterface)
                            {
                                txt = memberName + ": " + typeTxt + ";";
                            }
                            else if(isUndefined)
                            {
                                txt = GetAccessibility(member) + memberName + ": " + typeTxt + ";";
                            }
                            else
                            {
                                txt = GetAccessibility(member) + memberName + ": " + typeTxt + " = " + defaultValue + ";";
                            }
                            AddTxt(txt, result);
                        }
                        else if (member is IFieldSymbol fieldSymbol && IsValidField(fieldSymbol))
                        {
                            if (loadedFields.Contains(member.Name))
                            {
                                continue;
                            }
                            if (HasAttribute<NoTypescript>(member))
                            {
                                continue;
                            }
                            if (!IsValidField(fieldSymbol))
                            {
                                continue;
                            }
                            loadedFields.Add(member.Name);

                            string documentation = GetDocumentation(member);
                            if (documentation.Length > 0)
                            {
                                result.Add(documentation);
                            }

                            string memberName = member.Name;
                           
                            string typeTxt = GetTypeName(fieldSymbol.Type);
                            if (typeTxt.EndsWith("?"))
                            {
                                typeTxt = typeTxt.Substring(0, typeTxt.Length - 1);
                                typeTxt += " | null";
                            }
                            string defaultValue = GetDefaultValue(fieldSymbol, typeTxt);
                            bool isUndefined = false;
                            if (defaultValue == "undefined" && !memberName.EndsWith("?"))
                            {
                                isUndefined = true;
                                memberName += "!";
                            }

                            string txt = "";
                            if (isInterface)
                            {
                                txt = memberName + ": " + typeTxt + ";";
                            }
                            else if(isUndefined)
                            {
                                txt = GetAccessibility(member) + memberName + ": " + typeTxt + ";";
                            }
                            else
                            {
                                txt = GetAccessibility(member) + memberName + ": " + typeTxt + " = " + defaultValue + ";";
                            }
                            AddTxt(txt, result);
                        }
                    }
                }
            }
            GetContentAfter(result);
            return string.Join("\r\n", result);
        }
        protected virtual void DefineFullname(List<string> result)
        {
            if (IsConvertible)
            {

                string typeName = "\"" + Tools.GetFullName(type) + ", " + type.ContainingAssembly.Name + "\"";
                Type? realType = Tools.GetCompiledType(type.BaseType);
                if (realType != null && !realType.IsInterface && !realType.IsAbstract && realType != typeof(object))
                {
                    AddTxt("public static override get Fullname(): string { return " + typeName + "; }", result);
                }
                else
                {
                    AddTxt("public static get Fullname(): string { return " + typeName + "; }", result);
                }
            }
        }
        protected virtual void GetContentBefore(List<string> result)
        {

        }
        protected virtual void GetContentAfter(List<string> result)
        {

        }
        private string GetDefaultValue(ISymbol symbol, string type)
        {
            if (!(symbol is IPropertySymbol) && !(symbol is IFieldSymbol))
            {
                throw new Exception("Impossible");
            }
            if (type.EndsWith("[]"))
            {
                return "[]";
            }
            if(type.StartsWith("Map<"))
            {
                return "new " + type + "()";
            }

            string result = "undefined";
            var equalsSyntax = symbol.DeclaringSyntaxReferences[0].GetSyntax() switch
            {
                PropertyDeclarationSyntax property => property.Initializer,
                VariableDeclaratorSyntax variable => variable.Initializer,
                _ => null
            };
            if (equalsSyntax is not null)
            {
                result = equalsSyntax.Value.ToString();
            }
            if (result == "default")
            {
                result = "undefined";
            }
            return result;
        }

        protected virtual bool IsValidExtendsInterface(INamedTypeSymbol type)
        {
            return true;
        }
        protected virtual bool IsValidExtendsClass(INamedTypeSymbol type)
        {
            return true;
        }
        protected virtual bool IsValidImplements(INamedTypeSymbol type)
        {
            return true;
        }
        protected virtual bool IsValidField(IFieldSymbol type)
        {
            return true;
        }
        protected virtual bool IsValidProperty(IPropertySymbol type)
        {
            return true;
        }

        protected virtual bool CanConvert()
        {
            return true;
        }

        protected virtual void AddExtends(List<string> extends)
        {

        }
        protected virtual void AddImplements(List<string> implements)
        {

        }

    }
}
