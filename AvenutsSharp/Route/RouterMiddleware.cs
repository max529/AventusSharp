using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AventusSharp.Route.Attributes;
using AventusSharp.Route.Response;
using Microsoft.AspNetCore.Http;

namespace AventusSharp.Route
{
    public static class RouterMiddleware
    {
        private static Dictionary<Type, IRouter> routerInstances = new Dictionary<Type, IRouter>();
        private static Dictionary<string, RouterInfo> routesInfo = new Dictionary<string, RouterInfo>();

        public static void Configure(string viewDir)
        {
            View.directory = Path.Combine(Environment.CurrentDirectory, viewDir);
        }
        public static void Register(Assembly assembly)
        {
            if (View.directory == "")
            {
                View.directory = Path.Combine(Environment.CurrentDirectory, "Views");
            }
            List<Type> types = assembly.GetTypes().Where(p => p.GetInterfaces().Contains(typeof(IRouter))).ToList();
            Register(types);
        }

        public static void Register(IEnumerable<Type> types)
        {
            foreach (Type t in types)
            {
                if (routerInstances.ContainsKey(t))
                {
                    continue;
                }

                IRouter? routerTemp = (IRouter?)Activator.CreateInstance(t);
                if (routerTemp != null)
                {
                    routerInstances[t] = routerTemp;
                }

                if (!t.IsAbstract)
                {
                    List<MethodInfo> methods = t.GetMethods()
                                                .Where(p => p.GetCustomAttributes().Where(p1 => p1 is Attributes.Route).Count() > 0)
                                                .ToList();

                    foreach (MethodInfo method in methods)
                    {
                        List<RouterParameterInfo> fctParams = new List<RouterParameterInfo>();
                        ParameterInfo[] parameters = method.GetParameters();
                        foreach (ParameterInfo parameter in parameters)
                        {
                            fctParams.Add(new RouterParameterInfo(parameter.Name ?? "", parameter.ParameterType)
                            {
                                positionCSharp = parameter.Position,
                            });
                        }

                        List<Attribute> routesAttribute = new List<Attribute>();
                        List<Attribute> methodsAttribute = method.GetCustomAttributes().ToList();
                        List<MethodType> methodsToUse = new List<MethodType>();
                        foreach (Attribute methodAttribute in methodsAttribute)
                        {
                            if (methodAttribute is Attributes.Route) { routesAttribute.Add(methodAttribute); }
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
                        foreach (Attribute routeAttribute in routesAttribute)
                        {
                            foreach (MethodType methodType in methodsToUse)
                            {
                                Dictionary<string, RouterParameterInfo> param = fctParams.ToDictionary(p => p.name, p => p);
                                Attributes.Route r = (Attributes.Route)routeAttribute;

                                #region replace params
                                MatchCollection matching = new Regex("{.*?}").Matches(r.pattern);
                                string urlPattern = r.pattern;
                                int i = 0;
                                foreach (Match match in matching)
                                {
                                    string value = match.Value.Replace("{", "").Replace("}", "");
                                    if (param.ContainsKey(value))
                                    {
                                        if (param[value].type == typeof(int))
                                        {
                                            urlPattern = urlPattern.Replace(match.Value, "([0-9]+)");
                                        }
                                        else if (param[value].type == typeof(string))
                                        {
                                            urlPattern = urlPattern.Replace(match.Value, "([^/]+)");
                                        }
                                        param[value].positionUrl = i;
                                    }
                                    else
                                    {
                                        urlPattern = urlPattern.Replace(match.Value, "([^/]+)");
                                    }
                                    i++;
                                }
                                #endregion

                                #region replace function
                                MatchCollection matchingFct = new Regex("\\[.*?\\]").Matches(r.pattern);
                                if (matchingFct.Count > 0)
                                {
                                    foreach (Match match in matchingFct)
                                    {
                                        string value = match.Value.Replace("[", "").Replace("]", "");
                                        object? o = t.GetMethod(value, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance)?.Invoke(routerInstances[t], Array.Empty<object>());
                                        if (o != null)
                                        {
                                            urlPattern = urlPattern.Replace(match.Value, o.ToString());
                                        }
                                    }
                                }
                                #endregion


                                //urlPattern = "^/" + appName + urlPattern + "$";
                                urlPattern = "^" + urlPattern + "$";
                                urlPattern = urlPattern.Replace("/", "\\/").ToLower();


                                RouterInfo info = new RouterInfo(new Regex(urlPattern), methodType, method, routerInstances[t], parameters.Length);
                                info.parameters = param;


                                if (!routesInfo.ContainsKey(info.UniqueKey))
                                {
                                    Console.WriteLine("Add " + info.ToString());
                                    routesInfo.Add(info.UniqueKey, info);
                                }
                                else
                                {
                                    Console.WriteLine("Add " + info.ToString());
                                    RouterInfo otherInfo = routesInfo[info.UniqueKey];
                                    throw new Exception(info.ToString() + " is already added from " + otherInfo.action.Name + " (" + otherInfo.action.DeclaringType?.Assembly.FullName + ")");
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void Register()
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                Register(entry);
            }
        }


        public static async Task OnRequest(HttpContext context, Func<Task> next)
        {
            string url = context.Request.Path.ToString().ToLower();

            foreach (KeyValuePair<string, RouterInfo> routeInfo in routesInfo)
            {
                RouterInfo routerInfo = routeInfo.Value;

                if (routerInfo.method.ToString().ToLower() == context.Request.Method.ToLower())
                {
                    Match match = routerInfo.pattern.Match(url);
                    if (match.Success)
                    {
                        Console.WriteLine("trigger " + routerInfo.ToString());
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

                                        // check if body
                                        if (value == null)
                                        {
                                            value = await Form.Tools.ParseBody(context, parameter.type);
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
                            context.Response.StatusCode = 200;
                        }
                        else if (routerInfo.action.ReturnType == typeof(IResponse) || routerInfo.action.ReturnType.GetInterfaces().Contains(typeof(IResponse)))
                        {
                            IResponse? response = (IResponse?)routerInfo.action.Invoke(routerInfo.router, param);
                            if (response != null)
                            {
                                await response.send(context);
                            }
                        }
                        else
                        {
                            object? o = routerInfo.action.Invoke(routerInfo.router, param);
                            await new Json(o).send(context);
                        }
                        return;
                    }
                }
            }
            await next();
        }




    }
}
