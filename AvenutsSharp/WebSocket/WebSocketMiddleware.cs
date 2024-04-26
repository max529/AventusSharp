using AventusSharp.Routes;
using AventusSharp.Routes.Request;
using AventusSharp.WebSocket.Attributes;
using AventusSharp.WebSocket.Event;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public class WebSocketMiddleware
    {
        private static Dictionary<Type, IWsRoute> routeInstances = new Dictionary<Type, IWsRoute>();
        internal static readonly Dictionary<string, WsEndPoint> endPointInstances = new();
        private static WsEndPoint? mainEndPoint;


        private static Action<WebSocketConfig> configAction = (config) => { };
        private static bool configLoaded = false;
        internal static WebSocketConfig config = new WebSocketConfig();
        internal static Dictionary<Type, object> injected = new Dictionary<Type, object>();

        public static void Configure(Action<WebSocketConfig> configAction)
        {
            WebSocketMiddleware.configAction = configAction;

        }

        public static void Inject(object o)
        {
            injected[o.GetType()] = o;
        }


        public static async Task Stop() {
            foreach(KeyValuePair<string, WsEndPoint> endpoint in endPointInstances) {
                await endpoint.Value.Stop();
            }
        }
        public static VoidWithWsError Register()
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                return Register(entry);
            }
            VoidWithWsError result = new VoidWithWsError();
            result.Errors.Add(new WsError(WsErrorCode.CantDefineAssembly, "Can't determine the entry assembly"));
            return result;
        }

        public static VoidWithWsError Register(Assembly assembly)
        {
            List<Type> typesEndpoint = assembly.GetTypes().Where(p => p.GetInterfaces().Contains(typeof(IWsEndPoint))).ToList();
            List<Type> typesRoute = assembly.GetTypes().Where(p => p.GetInterfaces().Contains(typeof(IWsRoute))).ToList();
            return Register(typesEndpoint, typesRoute);
        }

        public static VoidWithWsError Register(IEnumerable<Type> typesEndpoint, IEnumerable<Type> typesRoute)
        {
            VoidWithWsError result;
            result = LoadConfig();
            if (!result.Success)
            {
                return result;
            }
            VoidWithWsError resultTemp = RegisterEndPoints(typesEndpoint);
            result.Errors.AddRange(resultTemp.Errors);
            RegisterRoutes(typesRoute);

            return result;
        }

        internal static WsEndPoint GetMain()
        {
            if (mainEndPoint == null)
            {
                mainEndPoint = new DefaultWsEndPoint();
                endPointInstances.Add(mainEndPoint.Path, mainEndPoint);
            }
            return mainEndPoint;
        }
        private static VoidWithWsError RegisterEndPoints(IEnumerable<Type> typesEndpoint)
        {
            VoidWithWsError result = new();
            foreach (Type t in typesEndpoint)
            {
                if (t.IsAbstract)
                {
                    continue;
                }

                WsEndPoint? endPoint = (WsEndPoint?)Activator.CreateInstance(t);
                if (endPoint != null)
                {
                    string path = endPoint.Path;
                    if (endPointInstances.ContainsKey(path))
                    {
                        continue;
                    }
                    endPointInstances[path] = endPoint;

                    if (endPoint.Main())
                    {
                        if (mainEndPoint == null)
                        {
                            mainEndPoint = endPoint;
                        }
                        else
                        {
                            string previous = mainEndPoint.GetType().FullName ?? "";
                            string current = endPoint.GetType().FullName ?? "";
                            result.Errors.Add(new WsError(WsErrorCode.MultipleMainEndpoint, "You can't define multiple main endpoint : " + previous + " and " + current));
                        }
                    }
                }
            }
            return result;
        }
        private static void RegisterRoutes(IEnumerable<Type> typesRoute)
        {
            Func<string, Dictionary<string, WebSocketRouterParameterInfo>, object, bool, string> transformPattern = config.transformPattern ?? PrepareUrl;
            Func<string, bool, Regex> transformPatternIntoRegex = config.transformPatternIntoRegex ?? PrepareRegex;

            foreach (Type t in typesRoute)
            {
                if (routeInstances.ContainsKey(t))
                {
                    continue;
                }

                if (t.IsAbstract)
                {
                    continue;
                }

                IWsRoute? routeTemp = (IWsRoute?)Activator.CreateInstance(t);
                if (routeTemp != null)
                {
                    routeInstances[t] = routeTemp;
                }

                // load end point class
                List<WsEndPoint> endpointsClass = new List<WsEndPoint>();
                List<Attribute> routeAttributes = t.GetCustomAttributes().ToList();
                Dictionary<Type, WsEndPoint> availables = endPointInstances.Values.ToDictionary(t => t.GetType(), t => t);

                foreach (Attribute routeAttribute in routeAttributes)
                {
                    if (routeAttribute is Attributes.EndPoint endPointAttr)
                    {
                        if (availables.ContainsKey(endPointAttr.endpoint))
                        {
                            WsEndPoint endpoint = availables[endPointAttr.endpoint];
                            if (!endpointsClass.Contains(endpoint))
                            {
                                endpointsClass.Add(endpoint);
                            }
                        }
                    }
                }

                List<MethodInfo> methods = t.GetMethods().ToList();

                foreach (MethodInfo method in methods)
                {
                    string fullName = method.DeclaringType?.Assembly.FullName ?? "";
                    if (!method.IsPublic || fullName.StartsWith("System."))
                    {
                        continue;
                    }

                    List<WebSocketRouterParameterInfo> fctParams = new List<WebSocketRouterParameterInfo>();
                    ParameterInfo[] parameters = method.GetParameters();
                    foreach (ParameterInfo parameter in parameters)
                    {
                        fctParams.Add(new WebSocketRouterParameterInfo(parameter.Name ?? "", parameter.ParameterType)
                        {
                            positionCSharp = parameter.Position,
                        });
                    }

                    List<Attribute> methodsAttribute = method.GetCustomAttributes().ToList();
                    WebSocketAttributeAnalyze infoMethod = PrepareAttributes(methodsAttribute);
                    
                    if (!infoMethod.canUse) continue;
                    
                    if (infoMethod.endPoints.Count == 0)
                    {
                        if (endpointsClass.Count == 0)
                        {
                            infoMethod.endPoints.Add(GetMain());
                        }
                        else
                        {
                            infoMethod.endPoints.Add(endpointsClass[0]);
                        }
                    }

                    if(infoMethod.pathes.Count == 0)
                    {
                        infoMethod.pathes.Add(Tools.GetDefaultMethodUrl(method));
                    }

                    Console.WriteLine("method " + method.Name + " => "+infoMethod.pathes.Count);
                    foreach (string route in infoMethod.pathes)
                    {
                        Dictionary<string, WebSocketRouterParameterInfo> @params = fctParams.ToDictionary(p => p.name, p => p);
                        string urlPattern = route;
                        string url = transformPattern(urlPattern, @params, routeInstances[t], false);
                        Regex regex = transformPatternIntoRegex(url, false);

                        foreach (IWsEndPoint endpointType in infoMethod.endPoints)
                        {
                            WebSocketRouteInfo info = new WebSocketRouteInfo(regex, method, routeInstances[t], parameters.Length, infoMethod.eventType, infoMethod.CustomFct);
                            info.parameters = @params;

                            if (endpointType is WsEndPoint _class)
                            {
                                if (!_class.routesInfo.ContainsKey(info.UniqueKey))
                                {
                                    info.router.AddEndPoint(_class);
                                    info.endpoint = _class;
                                    _class.routesInfo.Add(info.UniqueKey, info);
                                    if (config.PrintRoute)
                                        Console.WriteLine("Add websocket : " + info.ToString());
                                }
                                else
                                {
                                    if (config.PrintRoute)
                                        Console.WriteLine("Add websocket : " + info.ToString());
                                    WebSocketRouteInfo otherInfo = _class.routesInfo[info.UniqueKey];
                                    throw new Exception(info.ToString() + " is already added from " + otherInfo.action.Name + " (" + otherInfo.action.DeclaringType?.Assembly.FullName + ")");
                                }
                            }

                        }
                    }

                }
            }
        }

        internal static WebSocketAttributeAnalyze PrepareAttributes(IEnumerable<object> attrs)
        {
            WebSocketAttributeAnalyze info = new WebSocketAttributeAnalyze();
            Dictionary<Type, WsEndPoint> availables = endPointInstances.Values.ToDictionary(t => t.GetType(), t => t);
            foreach (object attr in attrs)
            {
                PrepareAttributesPart(attr, info, availables);
            }
            return info;
        }
        internal static WebSocketAttributeAnalyze PrepareAttributes(IEnumerable<Attribute> attrs)
        {
            WebSocketAttributeAnalyze info = new WebSocketAttributeAnalyze();
            Dictionary<Type, WsEndPoint> availables = endPointInstances.Values.ToDictionary(t => t.GetType(), t => t);
            foreach (object attr in attrs)
            {
                PrepareAttributesPart(attr, info, availables);
            }
            return info;
        }
        internal static void PrepareAttributesPart(object attr, WebSocketAttributeAnalyze info, Dictionary<Type, WsEndPoint> availables)
        {
            if (attr is Path pathAttr)
            {
                if (!info.pathes.Contains(pathAttr.pattern))
                {
                    info.pathes.Add(pathAttr.pattern);
                }
            }
            else if (attr is Attributes.EndPoint endPointAttr)
            {
                if (availables.ContainsKey(endPointAttr.endpoint))
                {
                    WsEndPoint endpoint = availables[endPointAttr.endpoint];
                    if (!info.endPoints.Contains(endpoint))
                    {
                        info.endPoints.Add(endpoint);
                    }
                }
            }
            else if(attr is ResponseType responseType)
            {
                if(info.eventType < responseType.Type)
                {
                    info.eventType = responseType.Type;
                }
                info.CustomFct = responseType.CustomFct;
            }
            else if(attr is NotRoute)
            {
                info.canUse = false;
            }
        }

        private static VoidWithWsError LoadConfig()
        {
            VoidWithWsError result = new();
            if (!configLoaded)
            {
                try
                {
                    configAction(config);
                }
                catch (Exception e)
                {
                    result.Errors.Add(new WsError(WsErrorCode.ConfigError, e));
                }
            }
            return result;
        }

        public static string PrepareUrl(string urlPattern, Dictionary<string, WebSocketRouterParameterInfo> @params, object o, bool isEvent)
        {
            urlPattern = ReplaceParams(urlPattern, @params);
            urlPattern = ReplaceFunction(urlPattern, o);
            return urlPattern;
        }
        public static string ReplaceFunction(string urlPattern, object o)
        {
            MatchCollection matchingFct = new Regex("\\[[a-zA-Z0-9_]*?\\]").Matches(urlPattern);

            if (matchingFct.Count > 0)
            {
                foreach (Match match in matchingFct)
                {
                    string value = match.Value.Replace("[", "").Replace("]", "");
                    MethodInfo? method = o.GetType().GetMethod(value, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (method == null)
                    {
                        Console.WriteLine("Can't find method " + value + " on " + o.GetType().FullName);
                        continue;
                    }
                    //object? res = method.Invoke(routeInstances[t], Array.Empty<object>());
                    object? res = method.Invoke(o, Array.Empty<object>());
                    if (res != null)
                    {
                        urlPattern = urlPattern.Replace(match.Value, res.ToString());
                    }
                }
            }
            return urlPattern;
        }
        public static string ReplaceParams(string urlPattern, Dictionary<string, WebSocketRouterParameterInfo> @params)
        {
            MatchCollection matching = new Regex("{.*?}").Matches(urlPattern);
            int i = 0;
            foreach (Match match in matching)
            {
                string value = match.Value.Replace("{", "").Replace("}", "");
                if (@params.ContainsKey(value))
                {
                    if (@params[value].type == typeof(int))
                    {
                        urlPattern = urlPattern.Replace(match.Value, "([0-9]+)");
                    }
                    else if (@params[value].type == typeof(string))
                    {
                        urlPattern = urlPattern.Replace(match.Value, "([^/]+)");
                    }
                    @params[value].positionUrl = i;
                }
                else
                {
                    urlPattern = urlPattern.Replace(match.Value, "([^/]+)");
                }
                i++;
            }
            return urlPattern;
        }

        public static Regex PrepareRegex(string urlPattern, bool isEvent)
        {
            if (!urlPattern.StartsWith("^"))
            {
                urlPattern = "^" + urlPattern;
            }
            if (!urlPattern.EndsWith("$"))
            {
                urlPattern += "$";
            }

            string replaceSlash = @"([a-zA-Z0-9_-]|^)\/";
            urlPattern = Regex.Replace(urlPattern, replaceSlash, "$1\\/");
            urlPattern = urlPattern.ToLower();
            return new Regex(urlPattern);
        }



        /// <summary>
        /// Middleware function
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async static Task OnRequest(HttpContext context, Func<Task> next)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                string newPath = context.Request.Path.ToString();
                if (endPointInstances.ContainsKey(newPath))
                {
                    System.Net.WebSockets.WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await endPointInstances[newPath].StartNewInstance(context, webSocket);
                }
                //else
                //{
                //    context.Response.StatusCode = 404;
                //    if (enableError)
                //    {
                //        Console.WriteLine("no router found for " + newPath);
                //        string listRouter = string.Join(", ", routers.Keys.ToList());
                //        Console.WriteLine("List " + listRouter);
                //    }
                //}
            }

            await next();
        }
    }
}
