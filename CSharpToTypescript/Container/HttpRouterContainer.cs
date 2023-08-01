using AventusSharp.Data;
using AventusSharp.Routes;
using AventusSharp.Routes.Attributes;
using AventusSharp.Routes.Response;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpToTypescript.Container
{
    internal class HttpRouterContainer : BaseContainer
    {
        public static bool Is(INamedTypeSymbol type, string fileName, out BaseContainer? result)
        {
            result = null;
            if (type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IRouter>(p)) != null)
            {
                if (Tools.ExportToTypesript(type, true))
                {
                    result = new HttpRouterContainer(type);
                }
                return true;
            }
            return false;
        }

        private List<HttpRouteContainer> routes = new List<HttpRouteContainer>();
        public Type realType;
        private List<Func<string>> additionalFcts = new();
        public string routePath = "";
        public HttpRouterContainer(INamedTypeSymbol type) : base(type)
        {
            string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (type.IsGenericType)
            {
                fullName += "`" + type.TypeParameters.Length;
            }
            Type? realType = ProjectManager.Config.compiledAssembly.GetType(fullName);
            if (realType == null)
            {
                throw new Exception("something went wrong");
            }
            this.realType = realType;
            foreach (ISymbol symbol in type.GetMembers())
            {

                if (symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind != MethodKind.Constructor)
                {
                    routes.Add(new HttpRouteContainer(methodSymbol, realType, this));
                }
            }

            ParentCheck();

            Attribute? pathAttr = realType.GetCustomAttributes().FirstOrDefault(p => p is TypescriptPath);
            if (pathAttr is TypescriptPath tpath)
            {
                routePath = tpath.path;
            }
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
            AddTxtOpen(GetAccessibilityExport(type) + GetAbstract() + "class " + GetTypeName(type, 0, true) + " " + GetExtension() + "{", result);
            result.Add(GetContent());
            AddTxtClose("}", result);
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                RemoveIndent();
            }

            return string.Join("\r\n", result);
        }


        private string GetContent()
        {
            List<string> result = new();
            Dictionary<string, string> functionNeeded = new Dictionary<string, string>();
            foreach (HttpRouteContainer route in routes)
            {
                if (route.canBeAdded)
                {
                    result.Add(route.Write());

                    foreach (KeyValuePair<string, Func<string>> fct in route.functionNeeded)
                    {
                        if (!functionNeeded.ContainsKey(fct.Key))
                        {
                            functionNeeded.Add(fct.Key, fct.Value());
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, string> fct in functionNeeded)
            {
                result.Add(fct.Value);
            }
            foreach (Func<string> fct in additionalFcts)
            {
                result.Add(fct());
            }
            return string.Join("\r\n\r\n", result);
        }

        private string GetAbstract()
        {
            if (type.IsAbstract)
            {
                return "abstract ";
            }
            return "";
        }

        private bool isParsingExtension = false;
        private string GetExtension()
        {
            string extend = "Aventus.HttpRoute";
            if (type.BaseType != null && type.BaseType.Name != "Object")
            {
                isParsingExtension = true;
                extend = GetTypeName(type.BaseType);
                isParsingExtension = false;
            }


            string txt = "extends " + extend + " ";

            return txt;
        }

        protected override string ParseGenericType(ITypeSymbol type, int depth, bool genericExtendsConstraint)
        {
            if (isParsingExtension)
            {
                if (type.TypeKind == TypeKind.Interface && type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IStorable>(p)) != null)
                {
                    Type? realType = Type.GetType(type.ContainingNamespace.ToString() + "." + type.Name + ", " + type.ContainingAssembly.Name);
                    if (realType != null)
                    {
                        List<Type> types = realType.Assembly.GetTypes().Where(t => t.IsClass && t.GetInterfaces().Contains(realType)).ToList();
                        if(types.Count > 0)
                        {
                            Type finalType = types[0];
                            foreach(Type typeTemp in types)
                            {
                                if (finalType.IsSubclassOf(typeTemp))
                                {
                                    finalType = typeTemp;
                                }
                            }

                            ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(finalType.FullName);
                            if(typeSymbol != null)
                            {
                                type = typeSymbol;
                            }
                        }
                        Console.WriteLine();
                    }
                    // Replace interface by real type
                }
            }
            return base.ParseGenericType(type, depth, genericExtendsConstraint);
        }


        private void ParentCheck()
        {
            if (type.IsAbstract)
            {
                return;
            }

            List<MethodInfo> methods = realType.GetMethods().ToList();
            Dictionary<string, MethodInfo> methodsByName = new();
            List<string> fcts = new List<string>();
            foreach (MethodInfo method in methods)
            {
                Attribute? route = method.GetCustomAttributes().ToList().Find(p => p is Route);
                if (route != null)
                {
                    Route r = (Route)route;
                    string urlPattern = r.pattern;
                    ParseFunctions(urlPattern, fcts);
                }
                else if (!methodsByName.ContainsKey(method.Name))
                {
                    methodsByName.Add(method.Name, method);
                }
            }
            IRouter? routerTemp = (IRouter?)Activator.CreateInstance(realType);
            foreach (string fct in fcts)
            {
                MethodInfo method = methodsByName[fct];
                object? o = method.Invoke(routerTemp, Array.Empty<object>());
                if (o != null)
                {
                    Func<string> getFct = () =>
                    {
                        List<string> resultTemp = new();
                        AddTxtOpen("public " + method.Name + "() {", resultTemp);
                        AddTxt("return \"" + o.ToString() + "\";", resultTemp);
                        AddTxtClose("}", resultTemp);
                        return string.Join("\r\n", resultTemp);
                    };
                    additionalFcts.Add(getFct);
                }
            }
        }
        private void ParseFunctions(string urlPattern, List<string> fcts)
        {
            MatchCollection matchingFct = new Regex("\\[.*?\\]").Matches(urlPattern);
            if (matchingFct.Count > 0)
            {
                foreach (Match match in matchingFct)
                {
                    string value = match.Value.Replace("[", "").Replace("]", "");
                    if (!fcts.Contains(value))
                    {
                        fcts.Add(value);
                    }
                }
            }
        }
    }


    internal class HttpRouteContainer
    {
        private IMethodSymbol methodSymbol;
        public bool canBeAdded = false;
        public string name = "";
        public MethodInfo method;
        public List<string> httpMethods = new List<string>() { };
        private Type @class;
        public Dictionary<string, string> parametersAndType = new();
        public Dictionary<string, Func<string>> functionNeeded = new();
        private HttpRouterContainer parent;
        public string route = "";
        private List<string?> listReturns = new();
        private Type typeContainer;

        public HttpRouteContainer(IMethodSymbol methodSymbol, Type @class, HttpRouterContainer parent)
        {
            this.parent = parent;
            this.methodSymbol = methodSymbol;
            this.@class = @class;
            this.name = methodSymbol.Name;
            MethodInfo? methodTemp = @class.GetMethod(name);
            if (methodTemp == null)
            {
                canBeAdded = false;
                return;
            }
            method = methodTemp;

            LoadHttpMethod(methodSymbol);
            if (!canBeAdded)
            {
                return;
            }
            if (httpMethods.Count == 0)
            {
                httpMethods.Add("get");
            }

            Dictionary<string, ParameterInfo> @params = method.GetParameters().ToDictionary(p => p.Name ?? "", p => p);
            foreach (KeyValuePair<string, ParameterInfo> pair in @params)
            {
                if (parametersAndType.ContainsKey(pair.Key))
                {
                    continue;
                }

                foreach (var parameter in methodSymbol.Parameters)
                {
                    if (parameter.Name == pair.Key)
                    {
                        parametersAndType.Add(pair.Key, parent.GetTypeName(parameter.Type));
                    }
                }

            }

            if (method.ReturnType.GetInterfaces().Contains(typeof(IResponse)))
            {
                typeContainer = method.ReturnType;
            }
            else
            {
                typeContainer = typeof(Json);
            }

            if (methodSymbol.DeclaringSyntaxReferences.Length > 0 && methodSymbol.DeclaringSyntaxReferences[0].GetSyntax() is MethodDeclarationSyntax methodSyntax)
            {
                if (methodSyntax.Body == null)
                {
                    canBeAdded = false;
                    return;
                }
                var temps = methodSyntax.Body.Statements;
                foreach (var temp in temps)
                {
                    LoadReturn(temp);
                }
            }
        }

        private void LoadHttpMethod(IMethodSymbol methodSymbol)
        {
            List<AttributeData> attrs = methodSymbol.GetAttributes().ToList();
            foreach (AttributeData attr in attrs)
            {
                if (attr.AttributeClass != null)
                {
                    if (attr.AttributeClass.ToString() == typeof(Get).FullName)
                    {
                        httpMethods.Add("get");
                    }
                    else if (attr.AttributeClass.ToString() == typeof(Post).FullName)
                    {
                        httpMethods.Add("post");
                    }
                    else if (attr.AttributeClass.ToString() == typeof(Put).FullName)
                    {
                        httpMethods.Add("put");
                    }
                    else if (attr.AttributeClass.ToString() == typeof(Delete).FullName)
                    {
                        httpMethods.Add("delete");
                    }
                    else if (attr.AttributeClass.ToString() == typeof(Options).FullName)
                    {
                        httpMethods.Add("options");
                    }
                    else if (attr.AttributeClass.ToString() == typeof(Route).FullName)
                    {
                        canBeAdded = true;
                        ParseRoute(attr);
                    }
                }

            }
        }

        private void ParseRoute(AttributeData route)
        {
            Dictionary<string, ParameterInfo> @params = method.GetParameters().ToDictionary(p => p.Name ?? "", p => p);
            if (route.ConstructorArguments.Length == 1)
            {
                string? txtRoute = route.ConstructorArguments[0].Value?.ToString();
                if (txtRoute != null)
                {
                    txtRoute = ParseParams(txtRoute, @params);
                    txtRoute = ParseFunctions(txtRoute);
                    this.route = txtRoute;
                    return;
                }
            }
            canBeAdded = false;
        }
        public string ParseParams(string urlPattern, Dictionary<string, ParameterInfo> @params)
        {
            MatchCollection matching = new Regex("{.*?}").Matches(urlPattern);
            foreach (Match match in matching)
            {
                string value = match.Value.Replace("{", "").Replace("}", "");
                if (parametersAndType.ContainsKey(value))
                {
                    continue;
                }
                if (@params.ContainsKey(value))
                {
                    if (@params[value].ParameterType == typeof(int))
                    {
                        parametersAndType.Add(value, "number");
                    }
                    else if (@params[value].ParameterType == typeof(string))
                    {
                        parametersAndType.Add(value, "string");
                    }
                    else
                    {
                        parametersAndType.Add(value, "any");
                    }
                }
                else
                {
                    parametersAndType.Add(value, "any");
                }
                urlPattern = urlPattern.Replace(match.Value, "${" + value + "}");
            }
            return urlPattern;
        }

        public string ParseFunctions(string urlPattern)
        {
            MatchCollection matchingFct = new Regex("\\[.*?\\]").Matches(urlPattern);
            if (matchingFct.Count > 0)
            {
                foreach (Match match in matchingFct)
                {
                    string value = match.Value.Replace("[", "").Replace("]", "");
                    MethodInfo? methodTemp = @class.GetMethod(value, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                    if (methodTemp == null)
                    {
                        canBeAdded = false;
                        return urlPattern;
                    }
                    if (!functionNeeded.ContainsKey(methodTemp.Name))
                    {
                        if (@class.IsAbstract)
                        {
                            Func<string> getTxt = () =>
                            {
                                return parent.GetIndentedText("public abstract " + methodTemp.Name + "(): string;");
                            };
                            functionNeeded.Add(methodTemp.Name, getTxt);
                        }
                        else
                        {
                            IRouter? routerTemp = (IRouter?)Activator.CreateInstance(@class);
                            object? o = methodTemp.Invoke(routerTemp, Array.Empty<object>());
                            if (o != null)
                            {
                                Func<string> getTxt = () =>
                                {
                                    List<string> resultTemp = new();
                                    parent.AddTxtOpen("public " + methodTemp.Name + "() {", resultTemp);
                                    parent.AddTxt("return \"" + o.ToString() + "\";", resultTemp);
                                    parent.AddTxtClose("}", resultTemp);
                                    return string.Join("\r\n", resultTemp);
                                };

                                functionNeeded.Add(methodTemp.Name, getTxt);
                            }
                        }
                    }

                    urlPattern = urlPattern.Replace(match.Value, "${this." + value + "()}");
                }
            }
            return urlPattern;
        }

        private void LoadReturn(SyntaxNode node)
        {
            if (node is ReturnStatementSyntax returnStatement)
            {
                if (returnStatement.Expression != null)
                {
                    if (returnStatement.Expression is BinaryExpressionSyntax)
                    {
                        listReturns.Add("bool");
                        return;
                    }
                    var temp = ProjectManager.Compilation.GetSemanticModel(node.SyntaxTree).GetSymbolInfo(returnStatement.Expression);
                    if (temp.Symbol is ILocalSymbol localSymbol)
                    {
                        listReturns.Add(parent.GetTypeName(localSymbol.Type));
                    }
                    else if (temp.Symbol is IMethodSymbol methodSymbol)
                    {
                        if (methodSymbol.MethodKind == MethodKind.Constructor && returnStatement.Expression is ObjectCreationExpressionSyntax cst)
                        {
                            string? name = methodSymbol.ContainingType.ToString();
                            if (name == typeof(Json).FullName)
                            {
                                GetTypeFirstParameterType(cst?.ArgumentList?.Arguments[0]);
                            }
                            else
                            {
                                listReturns.Add(parent.GetTypeName(methodSymbol.ContainingType));
                            }
                        }
                        else
                        {
                            listReturns.Add(parent.GetTypeName(methodSymbol.ReturnType));
                        }
                    }
                    else
                    {
                        Console.WriteLine("return type is " + temp.Symbol?.GetType());
                    }
                }
            }
            else
            {
                foreach (var child in node.ChildNodes())
                {
                    LoadReturn(child);
                }
            }
        }

        private void GetTypeFirstParameterType(ArgumentSyntax? argument)
        {
            if (argument == null)
            {
                return;
            }
            var argumentSymbol = ProjectManager.Compilation.GetSemanticModel(argument.SyntaxTree).GetSymbolInfo(argument.Expression);
            if (argumentSymbol.Symbol is ILocalSymbol localSymbol)
            {
                listReturns.Add(parent.GetTypeName(localSymbol.Type));
            }
            else if (argumentSymbol.Symbol is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.MethodKind == MethodKind.Constructor)
                {
                    listReturns.Add(parent.GetTypeName(methodSymbol.ContainingType));
                }
                else
                {
                    listReturns.Add(parent.GetTypeName(methodSymbol.ReturnType));
                }
            }
            else
            {
                Console.WriteLine("return type is " + argumentSymbol.Symbol?.GetType());
            }
        }

        public string Write()
        {
            List<string> result = new();

            string @params = string.Join(", ", parametersAndType.Select(p => p.Key + ": " + p.Value));
            parent.AddTxtOpen(BaseContainer.GetAccessibility(methodSymbol) + "async " + name + "(" + @params + ") {", result);
            parent.AddTxtOpen("let options: RequestInit = {", result);
            parent.AddTxt("method: Aventus.HttpMethod." + this.httpMethods[0].ToUpper(), result);
            parent.AddTxtClose("};", result);
            parent.AddTxt("let url = `" + route + "`;", result);

            if (typeContainer == typeof(Json))
            {
                if (listReturns.Count == 0)
                {
                    parent.AddTxt("return await this.router.request(url, options)", result);
                }
                else
                {
                    for (int i = 0; i < listReturns.Count; i++)
                    {
                        listReturns[i] = new Regex("Aventus\\.GenericResultWithError<(.*?)>").Replace(listReturns[i] ?? "", "$1");
                    }
                    string typeReturn = string.Join(" | ", listReturns);
                    parent.AddTxt("type TypeResult = " + typeReturn + ";", result);
                    parent.AddTxt("return await this.router.requestJSON<TypeResult>(url, options)", result);
                }

            }
            else if (typeContainer == typeof(TextResponse))
            {
                parent.AddTxt("return await this.router.requestTxt(url, options)", result);
            }


            parent.AddTxtClose("}", result);


            return string.Join("\r\n", result);
        }
    }
}
