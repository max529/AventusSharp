using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AventusSharp.Routes.Attributes;
using AventusSharp.Routes.Form;
using AventusSharp.Routes.Request;
using AventusSharp.Routes.Response;
using AventusSharp.Tools;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace AventusSharp.Routes
{
    public static class RouterMiddleware
    {
        private static Dictionary<Type, IRoute> routerInstances = new Dictionary<Type, IRoute>();
        private static Dictionary<string, RouteInfo> routesInfo = new Dictionary<string, RouteInfo>();
        private static Action<RouterConfig> configAction = (config) => { };
        private static bool configLoaded = false;
        internal static RouterConfig config = new RouterConfig();
        private static Dictionary<Type, object> injected = new Dictionary<Type, object>();

        public static void Configure(Action<RouterConfig> configAction)
        {
            RouterMiddleware.configAction = configAction;

        }

        public static void Register()
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                Register(entry);
            }
        }

        public static void Register(Assembly assembly)
        {
            List<Type> types = assembly.GetTypes().Where(p => p.GetInterfaces().Contains(typeof(IRoute))).ToList();
            Register(types);
        }

        public static void Register(IEnumerable<Type> types)
        {
            LoadConfig();
            Func<string, Dictionary<string, RouterParameterInfo>, Type, Regex> transformPattern = config.transformPattern ?? PrepareUrl;

            foreach (Type t in types)
            {
                if (routerInstances.ContainsKey(t))
                {
                    continue;
                }

                if (!t.IsAbstract)
                {
                    IRoute? routerTemp = (IRoute?)Activator.CreateInstance(t);
                    if (routerTemp != null)
                    {
                        routerInstances[t] = routerTemp;
                    }

                    List<MethodInfo> methods = t.GetMethods()
                                                //.Where(p => p.GetCustomAttributes().Where(p1 => p1 is Attributes.Path).Count() > 0)
                                                .ToList();

                    foreach (MethodInfo method in methods)
                    {
                        string fullName = method.DeclaringType?.Assembly.FullName ?? "";
                        if (!method.IsPublic || fullName.StartsWith("System."))
                        {
                            continue;
                        }
                        List<RouterParameterInfo> fctParams = new List<RouterParameterInfo>();
                        ParameterInfo[] parameters = method.GetParameters();
                        foreach (ParameterInfo parameter in parameters)
                        {
                            fctParams.Add(new RouterParameterInfo(parameter.Name ?? "", parameter.ParameterType)
                            {
                                positionCSharp = parameter.Position,
                            });
                        }

                        List<string> routes = new List<string>();
                        List<Attribute> methodsAttribute = method.GetCustomAttributes().ToList();
                        List<MethodType> methodsToUse = new List<MethodType>();
                        foreach (Attribute methodAttribute in methodsAttribute)
                        {
                            if (methodAttribute is Attributes.Path pathAttr) {
                                if (!routes.Contains(pathAttr.pattern))
                                {
                                    routes.Add(pathAttr.pattern);
                                }
                            }
                            else if (methodAttribute is Get) { methodsToUse.Add(MethodType.Get); }
                            else if (methodAttribute is Post) { methodsToUse.Add(MethodType.Post); }
                            else if (methodAttribute is Put) { methodsToUse.Add(MethodType.Put); }
                            else if (methodAttribute is Options) { methodsToUse.Add(MethodType.Options); }
                            else if (methodAttribute is Delete) { methodsToUse.Add(MethodType.Delete); }
                        }
                        if (methodsToUse.Count == 0)
                        {
                            methodsToUse.Add(MethodType.Get);
                        }
                        if(routes.Count == 0)
                        {
                            string defaultName = Tools.GetDefaultMethodUrl(method);
                            routes.Add(defaultName);
                        }
                        foreach (string route in routes)
                        {
                            foreach (MethodType methodType in methodsToUse)
                            {
                                Dictionary<string, RouterParameterInfo> @params = fctParams.ToDictionary(p => p.name, p => p);
                                string urlPattern = route;

                                Regex regex = transformPattern(urlPattern, @params, t);

                                RouteInfo info = new RouteInfo(regex, methodType, method, routerInstances[t], parameters.Length);
                                info.parameters = @params;


                                if (!routesInfo.ContainsKey(info.UniqueKey))
                                {
                                    if (config.PrintRoute)
                                        Console.WriteLine("Add " + info.ToString());
                                    routesInfo.Add(info.UniqueKey, info);
                                }
                                else
                                {
                                    if (config.PrintRoute)
                                        Console.WriteLine("Add " + info.ToString());
                                    RouteInfo otherInfo = routesInfo[info.UniqueKey];
                                    throw new Exception(info.ToString() + " is already added from " + otherInfo.action.Name + " (" + otherInfo.action.DeclaringType?.Assembly.FullName + ")");
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void Inject(object o)
        {
            injected[o.GetType()] = o;
        }
        public static Regex PrepareUrl(string urlPattern, Dictionary<string, RouterParameterInfo> @params, Type t)
        {
            urlPattern = ReplaceParams(urlPattern, @params);
            urlPattern = ReplaceFunction(urlPattern, t);
            Regex regex = PrepareRegex(urlPattern);
            return regex;
        }
        public static string ReplaceFunction(string urlPattern, Type t)
        {
            MatchCollection matchingFct = new Regex("\\[a-zA-Z0-9_*?\\]").Matches(urlPattern);
            if (matchingFct.Count > 0)
            {
                foreach (Match match in matchingFct)
                {
                    string value = match.Value.Replace("[", "").Replace("]", "");
                    MethodInfo? method = t.GetMethod(value, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if(method == null)
                    {
                        Console.WriteLine("Can't find method " + value + " on " + t.FullName);
                        continue;
                    }
                    object? o = method.Invoke(routerInstances[t], Array.Empty<object>());
                    if (o != null)
                    {
                        urlPattern = urlPattern.Replace(match.Value, o.ToString());
                    }
                }
            }
            return urlPattern;
        }
        public static string ReplaceParams(string urlPattern, Dictionary<string, RouterParameterInfo> @params)
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

        public static Regex PrepareRegex(string urlPattern)
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


        private static void LoadConfig()
        {
            if (!configLoaded)
            {
                configAction(config);
            }
        }

        public static async Task OnRequest(HttpContext context, Func<Task> next)
        {
            string url = context.Request.Path.ToString().ToLower();

            foreach (KeyValuePair<string, RouteInfo> routeInfo in routesInfo)
            {
                RouteInfo routerInfo = routeInfo.Value;

                if (routerInfo.method.ToString().ToLower() == context.Request.Method.ToLower())
                {
                    Match match = routerInfo.pattern.Match(url);
                    if (match.Success)
                    {
                        if (config.PrintTrigger)
                            Console.WriteLine("trigger " + routerInfo.ToString());
                        RouterBody? body = null;
                        object?[] param = new object[routerInfo.nbParamsFunction];
                        foreach (RouterParameterInfo parameter in routerInfo.parameters.Values)
                        {
                            if (parameter.positionCSharp != -1)
                            {
                                if (parameter.positionUrl == -1)
                                {
                                    if (parameter.type == typeof(HttpContext))
                                    {
                                        param[parameter.positionCSharp] = context;
                                    }
                                    else
                                    {
                                        object? value = null;
                                        // check if dependancies injection
                                        if (injected.ContainsKey(parameter.type))
                                        {
                                            value = injected[parameter.type];
                                        }
                                        // check if body
                                        else
                                        {
                                            if (body == null)
                                            {
                                                body = new RouterBody(context);
                                                VoidWithRouteError resultTemp = await body.Parse();
                                                if (!resultTemp.Success)
                                                {
                                                    context.Response.StatusCode = 422;
                                                    await new Json(resultTemp).send(context);
                                                    return;
                                                }
                                            }
                                            if (parameter.type == typeof(HttpFile))
                                            {
                                                value = body.GetFile();
                                            }
                                            else if (parameter.type == typeof(List<HttpFile>))
                                            {
                                                value = body.GetFiles();
                                            }
                                            else
                                            {
                                                ResultWithRouteError<object> bodyPart = body.GetData(parameter.type, parameter.name);
                                                if (!bodyPart.Success)
                                                {
                                                    context.Response.StatusCode = 422;
                                                    await new Json(bodyPart).send(context);
                                                    return;
                                                }
                                                value = bodyPart.Result;
                                            }

                                        }

                                        // error
                                        if (value == null)
                                        {
                                            Console.WriteLine("ERRRRROOOOOR");
                                        }
                                        param[parameter.positionCSharp] = value;
                                    }
                                }
                                else
                                {
                                    string value = match.Groups[parameter.positionUrl + 1].Value;
                                    try
                                    {
                                        param[parameter.positionCSharp] = Convert.ChangeType(value, parameter.type);
                                    }
                                    catch (Exception)
                                    {
                                        param[parameter.positionCSharp] = Form.Tools.DefaultValue(parameter.type);
                                    }

                                }
                            }
                        }
                        if (routerInfo.action.ReturnType == typeof(void))
                        {
                            routerInfo.action.Invoke(routerInfo.router, param);
                            context.Response.StatusCode = 204;
                        }
                        else
                        {
                            object? o = routerInfo.action.Invoke(routerInfo.router, param);
                            if (o is Task task)
                            {
                                task.Wait();
                                if (!o.GetType().IsGenericType)
                                {
                                    context.Response.StatusCode = 204;
                                    return;
                                }
                                o = o.GetType().GetProperty("Result")?.GetValue(o);
                            }

                            if (o is IResponse response)
                            {
                                await response.send(context);
                            }
                            else
                            {
                                await new Json(o).send(context);
                            }
                        }
                        return;
                    }
                }
            }
            await next();
        }




    }
}
