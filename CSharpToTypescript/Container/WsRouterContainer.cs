using AventusSharp.Tools.Attributes;
using AventusSharp.Tools;
using AventusSharp.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Text.RegularExpressions;
using AventusSharp.WebSocket.Attributes;
using AventusSharp.WebSocket.Event;
using Microsoft.AspNetCore.Http;
using System.Linq;
using AventusSharp.Routes.Attributes;
using AventusSharp.Data;
using System.Xml.Linq;

namespace CSharpToTypescript.Container
{
    internal class WsRouterContainer : BaseContainer
    {
        public static bool Is(INamedTypeSymbol type, string fileName, out BaseContainer? result)
        {
            result = null;
            if (type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IWsRoute>(p)) != null)
            {
                if (Tools.ExportToTypesript(type, ProjectManager.Config.exportWsRouteByDefault))
                {
                    result = new WsRouterContainer(type, fileName);
                }
                return true;
            }
            return false;
        }


        public Type realType;
        public string typescriptPath = "";
        public string fileName = "";
        private List<WsRouteContainer> routes = new List<WsRouteContainer>();
        private List<Func<string>> additionalFcts = new();
        public List<LocalEventInfo> routeEvents = new();

        public WsRouterContainer(INamedTypeSymbol type, string fileName) : base(type)
        {
            this.fileName = fileName;
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
                    routes.Add(new WsRouteContainer(methodSymbol, realType, this));
                }
            }
            ParentCheck();
            ParseAttributes();
        }

        private void ParseAttributes()
        {
            IEnumerable<Attribute> attrs = realType.GetCustomAttributes();
            bool oneEndPoint = false;
            foreach (Attribute attr in attrs)
            {
                if (attr is EndPoint endPointAttr)
                {
                    if (!WsEndPointContainer._routers.ContainsKey(endPointAttr.endpoint))
                    {
                        WsEndPointContainer._routers[endPointAttr.endpoint] = new();
                    }

                    WsEndPointContainer._routers[endPointAttr.endpoint].Add(new WsEndPointContainerInfo(type, endPointAttr.typescriptPath));
                    oneEndPoint = true;
                }
            }

            if (!oneEndPoint)
            {
                WsEndPointContainer._defaultRouters.Add(new WsEndPointContainerInfo(type));
            }
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
                        if (types.Count > 0)
                        {
                            Type finalType = types[0];
                            foreach (Type typeTemp in types)
                            {
                                if (finalType.IsSubclassOf(typeTemp))
                                {
                                    finalType = typeTemp;
                                }
                            }

                            ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(finalType.FullName ?? "");
                            if (typeSymbol != null)
                            {
                                type = typeSymbol;
                            }
                        }
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
                string fullName = method.DeclaringType?.Assembly.FullName ?? "";
                if (!method.IsPublic || fullName.StartsWith("System."))
                {
                    continue;
                }
                string urlPattern = "";
                Attribute? route = method.GetCustomAttributes().ToList().Find(p => p is AventusSharp.WebSocket.Attributes.Path);
                if (route != null)
                {
                    AventusSharp.WebSocket.Attributes.Path r = (AventusSharp.WebSocket.Attributes.Path)route;
                    urlPattern = r.pattern;
                }
                else
                {
                    urlPattern = AventusSharp.WebSocket.Tools.GetDefaultMethodUrl(method);
                }
                ParseFunctions(urlPattern, fcts);
            }
            IWsRoute? routerTemp = (IWsRoute?)Activator.CreateInstance(realType);
            foreach (string fct in fcts)
            {
                MethodInfo? method = realType.GetMethod(fct, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                object? o = method.Invoke(routerTemp, Array.Empty<object>());
                if (method == null)
                {
                    throw new Exception("Can't find method " + fct + " on " + realType.FullName);
                }
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
            result.Add(GetEventsDef());

            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                RemoveIndent();
            }

            return string.Join("\r\n", result);
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
            string extend = "AventusSharp.WsRoute";
            if (type.BaseType != null && type.BaseType.Name != "Object")
            {
                isParsingExtension = true;
                extend = GetTypeName(type.BaseType);
                isParsingExtension = false;
            }


            string txt = "extends " + extend + " ";

            return txt;
        }

        private string GetContent()
        {
            List<string> result = new();
            Dictionary<string, string> functionNeeded = new Dictionary<string, string>();

            if (routeEvents.Count > 0)
            {
                List<string> eventList = new List<string>();
                AddTxt("", eventList);
                AddTxtOpen("public events: {", eventList);
                foreach (LocalEventInfo routeEvent in routeEvents)
                {
                    AddTxt(routeEvent.fctName + ": " + routeEvent.name + ",", eventList);
                }
                AddTxtClose("}", eventList);

                AddTxt("", eventList);
                string endPointType = GetTypeName(typeof(WsEndPoint));
                AddTxtOpen("public constructor(endpoint: "+ endPointType + ") {", eventList);
                AddTxt("super(endpoint);", eventList);
                AddTxtOpen("this.events = {", eventList);
                foreach (LocalEventInfo routeEvent in routeEvents)
                {
                    AddTxt(routeEvent.fctName + ": new " + routeEvent.name + "(endpoint),", eventList);
                }
                AddTxtClose("};", eventList);
                AddTxtClose("}", eventList);

                result.Add(string.Join("\r\n", eventList));
            }

            foreach (WsRouteContainer route in routes)
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


        private string GetEventsDef()
        {
            List<string> eventContent = new List<string>();
            string parentName = GetTypeName(typeof(WsEvent<>)).Split("<")[0];
            foreach (LocalEventInfo routeEvent in routeEvents)
            {
                AddTxt(" ", eventContent);
                AddTxtOpen("export class " + routeEvent.name + " extends " + parentName + "<" + routeEvent.type + "> {", eventContent);
                AddTxt("/**", eventContent);
                AddTxt(" * @inheritdoc", eventContent);
                AddTxt(" */", eventContent);
                AddTxtOpen("protected override get path(): string {", eventContent);
                AddTxt("return \"" + routeEvent.path + "\";", eventContent);
                AddTxtClose("}", eventContent);
                AddTxtClose("}", eventContent);
            }

            return string.Join("\r\n", eventContent);
        }

        protected override string? CustomReplacer(ISymbol type, string fullname, string? result)
        {
            return applyReplacer(ProjectManager.Config.replacer.wsRouter, fullname, result);
        }
    }

    internal class WsRouteContainer
    {
        private IMethodSymbol methodSymbol;
        public bool canBeAdded = false;
        public string name = "";
        public MethodInfo method;
        private Type @class;
        public Dictionary<string, string> parametersBodyAndType = new();
        public Dictionary<string, string> parametersUrlAndType = new();
        public Dictionary<string, Func<string>> functionNeeded = new();
        private WsRouterContainer parent;
        public string route = "";
        public string returnType = "";

        public WsRouteContainer(IMethodSymbol methodSymbol, Type @class, WsRouterContainer parent)
        {
            this.parent = parent;
            this.methodSymbol = methodSymbol;
            this.@class = @class;
            this.name = methodSymbol.Name;
            MethodInfo? methodTemp = @class.GetMethod(name);
            if (methodTemp == null || !methodTemp.IsPublic)
            {
                canBeAdded = false;
                return;
            }
            method = methodTemp;
            canBeAdded = true;
            LoadWsAttributes(methodSymbol);
            LoadMethodParameters();



            if (method.ReturnType != typeof(void))
            {
                Type typeTemp = method.ReturnType;
                bool isTask = false;
                if (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    typeTemp = typeTemp.GetGenericArguments()[0];
                    isTask = true;
                }

                if (typeTemp.GetInterfaces().Contains(typeof(IWebSocketEvent)))
                {
                    // just return the content
                    LoadEventBody(typeTemp);
                }
                else
                {
                    if (isTask && methodSymbol.ReturnType is INamedTypeSymbol namedType)
                    {
                        returnType = parent.GetTypeName(namedType.TypeArguments[0]);
                    }
                    else
                    {
                        returnType = parent.GetTypeName(methodSymbol.ReturnType);
                    }
                    // create the event associated

                    string parentName = parent.GetTypeName(parent.type);
                    string[] splitted = parentName.Split("<");
                    splitted[0] += "_" + this.name;
                    this.parent.routeEvents.Add(new LocalEventInfo()
                    {
                        name = string.Join("<", splitted),
                        fctName = name,
                        type = returnType,
                        path = route
                    });
                }
            }
        }

        private void LoadMethodParameters()
        {
            List<string> knownParameters = new List<string>() {
                 typeof(WebSocketConnection).FullName ?? "",
                 typeof(WsEndPoint).FullName ?? "",
                 typeof(HttpContext).FullName ?? "",
                 typeof(System.Net.WebSockets.WebSocket).FullName ?? "",
            };
            Dictionary<string, ParameterInfo> @params = method.GetParameters().ToDictionary(p => p.Name ?? "", p => p);
            foreach (KeyValuePair<string, ParameterInfo> pair in @params)
            {
                if (parametersUrlAndType.ContainsKey(pair.Key))
                {
                    continue;
                }

                foreach (var parameter in methodSymbol.Parameters)
                {
                    if (parameter.Name == pair.Key)
                    {
                        if (knownParameters.Contains(parameter.Type.ToString() ?? ""))
                        {
                            continue;
                        }
                        if (Tools.HasAttribute<NoTypescript>(parameter))
                        {
                            continue;
                        }
                        parametersBodyAndType.Add(pair.Key, parent.GetTypeName(parameter.Type));
                    }
                }

            }

        }
        private void LoadWsAttributes(IMethodSymbol methodSymbol)
        {
            List<AttributeData> attrs = methodSymbol.GetAttributes().ToList();
            foreach (AttributeData attr in attrs)
            {
                if (attr.AttributeClass != null)
                {
                    if (attr.AttributeClass.ToString() == typeof(AventusSharp.WebSocket.Attributes.Path).FullName)
                    {
                        Dictionary<string, ParameterInfo> @params = method.GetParameters().ToDictionary(p => p.Name ?? "", p => p);
                        if (attr.ConstructorArguments.Length == 1)
                        {
                            ParseRoute(attr.ConstructorArguments[0].Value?.ToString(), @params);
                        }
                    }
                }

            }

            if (route == "")
            {
                string defaultName = AventusSharp.Routes.Tools.GetDefaultMethodUrl(method);
                ParseRoute(defaultName, new Dictionary<string, ParameterInfo>());
            }
        }

        private void ParseRoute(string? txtRoute, Dictionary<string, ParameterInfo> @params)
        {
            if (txtRoute != null)
            {
                txtRoute = ParseParams(txtRoute, @params);
                txtRoute = ParseFunctions(txtRoute);
                this.route = txtRoute;
                return;
            }

        }
        public string ParseParams(string urlPattern, Dictionary<string, ParameterInfo> @params)
        {
            MatchCollection matching = new Regex("{.*?}").Matches(urlPattern);
            foreach (Match match in matching)
            {
                string value = match.Value.Replace("{", "").Replace("}", "");
                if (parametersUrlAndType.ContainsKey(value))
                {
                    continue;
                }
                if (@params.ContainsKey(value))
                {
                    if (@params[value].ParameterType == typeof(int))
                    {
                        parametersUrlAndType.Add(value, "number");
                    }
                    else if (@params[value].ParameterType == typeof(string))
                    {
                        parametersUrlAndType.Add(value, "string");
                    }
                    else
                    {
                        parametersUrlAndType.Add(value, "any");
                    }
                }
                else
                {
                    parametersUrlAndType.Add(value, "any");
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
                    MethodInfo? methodTemp = @class.GetMethod(value, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                            IWsRoute? routerTemp = (IWsRoute?)Activator.CreateInstance(@class);
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

        private void LoadEventBody(Type type)
        {
            while (type.BaseType != null)
            {
                Type newType = type.BaseType;
                if (newType.IsGenericType && newType.GetGenericTypeDefinition() == typeof(WsEvent<>))
                {
                    Type body = newType.GetGenericArguments()[0];

                    if (body.IsNested)
                    {
                        ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(body.FullName ?? "");
                        if (typeSymbol is INamedTypeSymbol namedType)
                        {
                            returnType = parent.GetTypeName(namedType);
                        }
                    }
                }

                type = newType;
            }
        }

        public string Write()
        {
            List<string> result = new();
            string bodyKey = "body";
            if (parametersBodyAndType.Count > 0)
            {
                int i = 0;
                while (parametersUrlAndType.ContainsKey(bodyKey))
                {
                    bodyKey = "body" + i;
                    i++;
                }

                parametersUrlAndType[bodyKey] = "{ " + string.Join(", ", parametersBodyAndType.Select(p => p.Key + ": " + p.Value)) + " } | FormData";
            }

            string @params = string.Join(", ", parametersUrlAndType.Select(p => p.Key + ": " + p.Value));

            parent.AddTxtOpen(BaseContainer.GetAccessibility(methodSymbol) + "async " + name + "(" + @params + ") {", result);

            string fctTxt = "";
            string genericResult = "";
            if (returnType != "")
            {
                fctTxt += "return ";
                genericResult = "<" + returnType + ">";
            }
            if (parametersBodyAndType.Count > 0)
            {
                fctTxt += "await this.endpoint.sendMessageAndWait" + genericResult + "(`" + route + "`, " + bodyKey + ");";
            }
            else
            {
                fctTxt += "await this.endpoint.sendMessageAndWait" + genericResult + "(`" + route + "`);";
            }

            parent.AddTxt(fctTxt, result);
            parent.AddTxtClose("}", result);

            return string.Join("\r\n", result);
        }
    }

    public class LocalEventInfo
    {
        public string name;
        public string type;
        public string path;
        public string fctName;
    }
}
