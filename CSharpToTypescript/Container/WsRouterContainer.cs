﻿using AventusSharp.Tools.Attributes;
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
using MySqlX.XDevAPI.Common;
using System.Net;
using EndPoint = AventusSharp.WebSocket.Attributes.EndPoint;

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


        public string typescriptPath = "";
        public string fileName = "";
        private List<WsRouteContainer> routes = new List<WsRouteContainer>();
        private List<Func<string>> additionalFcts = new();
        public List<LocalEventInfo> routeEvents = new();

        public Type? endPoint;

        public WsRouterContainer(INamedTypeSymbol type, string fileName) : base(type)
        {
            this.fileName = fileName;

            foreach (ISymbol symbol in type.GetMembers())
            {

                if (symbol is IMethodSymbol methodSymbol && methodSymbol.MethodKind != MethodKind.Constructor && !methodSymbol.IsExtern && !methodSymbol.IsStatic)
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
            foreach (Attribute attr in attrs)
            {
                if (attr is EndPoint endPointAttr)
                {
                    endPoint = endPointAttr.endpoint;
                }
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
                object? o = method?.Invoke(routerTemp, Array.Empty<object>());
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

            string prefix = ProjectManager.Config.wsEndpoint.prefix;
            if (prefix != "")
            {
                Func<string> getFct = () =>
                {
                    List<string> resultTemp = new();
                    AddTxt("", resultTemp);
                    AddTxtOpen("public override getPrefix(): string {", resultTemp);
                    AddTxt("return \"" + prefix + "\";", resultTemp);
                    AddTxtClose("}", resultTemp);
                    return string.Join("\r\n", resultTemp);
                };
                additionalFcts.Add(getFct);
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

            if (routeEvents.Count > 0 || endPoint != null)
            {
                List<string> eventList = new List<string>();
                if (routeEvents.Count > 0)
                {
                    AddTxt("", eventList);
                    AddTxtOpen("public events: {", eventList);
                    foreach (LocalEventInfo routeEvent in routeEvents)
                    {
                        AddTxt(routeEvent.name + ": " + routeEvent.fctName + ",", eventList);
                    }
                    AddTxtClose("}", eventList);
                }

                AddTxt("", eventList);
                AddTxtOpen("public constructor(endpoint?: " + GetTypeName(typeof(WsEndPoint)) + ") {", eventList);
                if (endPoint != null)
                {
                    string endPointType = GetTypeName(endPoint);
                    AddTxt("super(endpoint ?? " + endPointType + ".getInstance());", eventList);
                }
                else
                {
                    AddTxt("super(endpoint);", eventList);
                }
                if (routeEvents.Count > 0)
                {
                    AddTxtOpen("this.events = {", eventList);
                    foreach (LocalEventInfo routeEvent in routeEvents)
                    {
                        if (routeEvent.functionNeeded.Count == 0)
                        {
                            AddTxt(routeEvent.name + ": new " + routeEvent.fctName + "(this.endpoint, this.getPrefix),", eventList);
                        }
                        else
                        {
                            string txt = string.Join(", ", routeEvent.functionNeeded.Select(p => "this." + p.Key));
                            AddTxt(routeEvent.name + ": new " + routeEvent.fctName + "(this.endpoint, this.getPrefix, " + txt + "),", eventList);
                        }
                    }
                    AddTxtClose("};", eventList);
                    AddTxt("", eventList);
                    AddTxtOpen("for(let key in this.events) {", eventList);
                    AddTxt("this.events[key].init();", eventList);
                    AddTxtClose("}", eventList);
                }
                
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
                if (routeEvent.isExisting) continue;

                AddTxt(" ", eventContent);
                AddTxtOpen("export class " + routeEvent.fctName + " extends " + parentName + "<" + routeEvent.type + "> {", eventContent);
                if (routeEvent.functionNeeded.Count > 0)
                {
                    AddTxt("", eventContent);
                    List<string> cstParams = new List<string>();
                    foreach (KeyValuePair<string, Func<string>> fct in routeEvent.functionNeeded)
                    {
                        AddTxt("public " + fct.Key + ": () => string;", eventContent);
                        cstParams.Add(fct.Key + ": () => string");
                    }

                    AddTxtOpen("public constructor(endpoint: " + GetTypeName(typeof(WsEndPoint)) + ", getPrefix: () => string, " + string.Join(", ", cstParams) + ") {", eventContent);
                    AddTxt("super(endpoint, getPrefix);", eventContent);
                    foreach (KeyValuePair<string, Func<string>> fct in routeEvent.functionNeeded)
                    {
                        AddTxt("this." + fct.Key + " = " + fct.Key + ";", eventContent);
                    }
                    AddTxtClose("}", eventContent);
                }
                AddTxt("", eventContent);
                AddTxt("/**", eventContent);
                AddTxt(" * @inheritdoc", eventContent);
                AddTxt(" */", eventContent);
                AddTxtOpen("protected override path(): string {", eventContent);
                AddTxt("return `${this.getPrefix()}" + routeEvent.path + "`;", eventContent);
                AddTxtClose("}", eventContent);
                if (routeEvent.listenOnBoot != null)
                {
                    string trueTxt = routeEvent.listenOnBoot == true ? "true" : "false";
                    AddTxt("", eventContent);
                    AddTxt("/**", eventContent);
                    AddTxt(" * @inheritdoc", eventContent);
                    AddTxt(" */", eventContent);
                    AddTxtOpen("protected override listenOnBoot(): boolean {", eventContent);
                    AddTxt("return " + trueTxt + ";", eventContent);
                    AddTxtClose("}", eventContent);
                }
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
        public MethodInfo? method;
        private Type @class;
        public Dictionary<string, string> parametersBodyAndType = new();
        public Dictionary<string, string> parametersUrlAndType = new();
        public Dictionary<string, Func<string>> functionNeeded = new();
        private WsRouterContainer parent;
        public string route = "";
        public string routeEvent = "";
        public string returnType = "";
        private bool? listenOnBoot = null;

        public WsRouteContainer(IMethodSymbol methodSymbol, Type @class, WsRouterContainer parent)
        {
            this.parent = parent;
            this.methodSymbol = methodSymbol;
            this.@class = @class;
            this.name = methodSymbol.Name;
            MethodInfo? methodTemp = Tools.GetMethodInfo(methodSymbol, @class);
            if (methodTemp == null || !methodTemp.IsPublic)
            {
                canBeAdded = false;
                return;
            }
            method = methodTemp;
            canBeAdded = true;
            LoadWsAttributes(methodTemp);
            LoadMethodParameters();
            LoadReturnType();
        }

        private void LoadMethodParameters()
        {
            if (method == null)
            {
                return;
            }
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
        private void LoadWsAttributes(MethodInfo methodSymbol)
        {
            if (method == null)
            {
                return;
            }
            IEnumerable<Attribute> attrs = methodSymbol.GetCustomAttributes();
            foreach (Attribute attr in attrs)
            {
                if (attr is AventusSharp.WebSocket.Attributes.Path attrPath)
                {
                    Dictionary<string, ParameterInfo> @params = method.GetParameters().ToDictionary(p => p.Name ?? "", p => p);
                    ParseRoute(attrPath.pattern, @params);

                }
                else if (attr is ListenOnBoot attrListenOnBoot)
                {
                    listenOnBoot = attrListenOnBoot.listen;
                }
                else if (attr is NotRoute)
                {
                    canBeAdded = false;
                }
            }

            if (route == "")
            {
                string defaultName = AventusSharp.Routes.Tools.GetDefaultMethodUrl(method);
                ParseRoute(defaultName, new Dictionary<string, ParameterInfo>());
            }
        }

        private void LoadReturnType()
        {
            if (method == null)
            {
                return;
            }

            Type typeTemp = method.ReturnType;
            if (typeTemp == typeof(Task))
            {
                typeTemp = typeof(void);
            }
            else if (typeTemp.IsGenericType && typeTemp.GetGenericTypeDefinition() == typeof(Task<>))
            {
                typeTemp = typeTemp.GetGenericArguments()[0];
            }

            Type? realType;
            if (Tools.IsSubclass(typeof(ResultWithError<,>), typeTemp, out realType))
            {
                if (realType != null)
                {
                    typeTemp = realType.GenericTypeArguments[0];
                }
            }
            else if (Tools.IsSubclass(typeof(VoidWithError<>), typeTemp))
            {
                typeTemp = typeof(void);
            }


            if (typeTemp != typeof(void))
            {
                bool isExisting = false;
                Dictionary<string, Func<string>> fcts = functionNeeded;
                string fctName;
                if (typeTemp.GetInterfaces().Contains(typeof(IWebSocketEvent)))
                {
                    // just return the content
                    LoadEventBody(typeTemp);
                    isExisting = true;
                    INamedTypeSymbol symbol = Tools.GetNameTypeSymbol(typeTemp);
                    if (!WsEventContainer.CreatedEvents.ContainsKey(symbol))
                    {
                        WsEventContainer eventContainer = new WsEventContainer(symbol, null);
                        fcts = new Dictionary<string, Func<string>>();
                        foreach (string fctNameTemp in eventContainer.fctsConstructor)
                        {
                            if (!functionNeeded.ContainsKey(fctNameTemp))
                            {
                                functionNeeded.Add(fctNameTemp, () => parent.GetIndentedText("public abstract " + fctNameTemp + "(): string;"));
                            }
                            fcts.Add(fctNameTemp, () => "");
                        }
                    }
                    fctName = parent.GetTypeName(typeTemp);
                }
                else
                {
                    returnType = parent.GetTypeName(typeTemp);
                    // create the event associated
                    string parentName = parent.GetTypeName(parent.type);
                    string[] splitted = parentName.Split("<");
                    splitted[0] += "_" + this.name;
                    fctName = string.Join("<", splitted);

                }

                this.parent.routeEvents.Add(new LocalEventInfo(
                    name: name,
                    fctName: fctName,
                    type: returnType,
                    path: routeEvent,
                    listenOnBoot: listenOnBoot,
                    functionNeeded: fcts,
                    isExisting: isExisting
                ));
            }
        }

        private void ParseRoute(string? txtRoute, Dictionary<string, ParameterInfo> @params)
        {
            if (txtRoute != null)
            {
                txtRoute = ParseParams(txtRoute, @params);
                txtRoute = ParseFunctions(txtRoute);
                this.route = txtRoute;
                this.routeEvent = txtRoute;
                foreach (KeyValuePair<string, string> parameterUrlAndType in parametersUrlAndType)
                {
                    string type = parameterUrlAndType.Value == "number" ? "number" : "string";
                    this.routeEvent = this.routeEvent.Replace("${" + parameterUrlAndType.Key + "}", "{" + parameterUrlAndType.Key + ":" + type + "}");
                }

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

        private string GetUniqueParamName(string name)
        {
            string key = name;
            int i = 0;
            while (parametersUrlAndType.ContainsKey(key))
            {
                key = name + i;
                i++;
            }
            return key;
        }

        public string Write()
        {
            List<string> result = new();
            string bodyKey = GetUniqueParamName("body");
            if (parametersBodyAndType.Count > 0)
            {
                parametersUrlAndType[bodyKey] = "{ " + string.Join(", ", parametersBodyAndType.Select(p => p.Key + ": " + p.Value)) + " } | FormData";
            }
            string optionsKey = GetUniqueParamName("options");
            string infoType = "";
            string extractType = "";
            if (ProjectManager.CompilingAventusSharp)
            {
                parametersUrlAndType[optionsKey] = "WsRouteSendOptions = {}";
                infoType = "SocketSendMessageOptions";
                this.parent.addImport(".\\AventusJs\\src\\WebSocket\\ISocket.lib.avt", "type SocketSendMessageOptions");
                this.parent.addImport(".\\AventusJs\\src\\WebSocket\\Route.lib.avt", "type WsRouteSendOptions");
                this.parent.addImport(".\\AventusJs\\src\\WebSocket\\Route.lib.avt", "type ExtractResponse");
                extractType = "ExtractResponse";
            }
            else
            {
                parametersUrlAndType[optionsKey] = "AventusSharp.WebSocket.WsRouteSendOptions = {}";
                infoType = "AventusSharp.WebSocket.SocketSendMessageOptions";
                extractType = "AventusSharp.ExtractResponse";

            }
            string @params = string.Join(", ", parametersUrlAndType.Select(p => p.Key + ": " + p.Value));

            string resultType = "any";
            string fctTxt = "";

            LocalEventInfo? _event = parent.routeEvents.Find(p => p.name == name);
            if (_event != null)
            {
                resultType = "Promise<" + extractType + "<" + _event.fctName + ">>";
                fctTxt = "return await this.endpoint.sendMessageAndWait<" + extractType + "<" + _event.fctName + ">>(info);";
            }
            else
            {
                fctTxt = "return await this.endpoint.sendMessage(info);";
                resultType = "Promise<Aventus.VoidWithError<Aventus.GenericError<number>>>";
            }
            if (returnType != "")
            {
                //fctTxt = "return await this.endpoint.sendMessageAndWait<" + returnType + ">(info);";
                //resultType = "Promise<Aventus.ResultWithError<" + returnType + ", Aventus.GenericError<number>>>";
            }
            else
            {
                //fctTxt = "return await this.endpoint.sendMessage(info);";
                //resultType = "Promise<Aventus.VoidWithError<Aventus.GenericError<number>>>";

            }

            string fctDesc = BaseContainer.GetAccessibility(methodSymbol) + "async " + name + "(" + @params + "): " + resultType + " {";

            parent.AddTxtOpen(fctDesc, result);

            parent.AddTxtOpen("const info: " + infoType + " = {", result);
            parent.AddTxt("channel: `${this.getPrefix()}" + route + "`,", result);
            if (parametersBodyAndType.Count > 0)
            {
                parent.AddTxt("body: " + bodyKey + ",", result);
            }
            parent.AddTxt("..." + optionsKey + ",", result);
            parent.AddTxtClose("};", result);



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
        public bool? listenOnBoot;
        public Dictionary<string, Func<string>> functionNeeded = new Dictionary<string, Func<string>>();
        public bool isExisting;

        public LocalEventInfo(string name, string type, string path, string fctName, bool? listenOnBoot, Dictionary<string, Func<string>> functionNeeded, bool isExisting)
        {
            this.name = name;
            this.type = type;
            this.path = path;
            this.fctName = fctName;
            this.listenOnBoot = listenOnBoot;
            this.functionNeeded = functionNeeded;
            this.isExisting = isExisting;
        }
    }
}
