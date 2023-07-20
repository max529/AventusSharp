using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    /// <summary>
    /// Entrie point for the lib, it's a middleware
    /// </summary>
    public static class WebSocketMiddleware
    {
        /// <summary>
        /// enable error after being ready
        /// </summary>
        public readonly static bool enableError = false;
        private static readonly Dictionary<string, IWebSocketInstance> routers = new();
        /// <summary>
        /// Create all Websocket Instance inside current Assembly + load all routes inside namespace $CURRENT.Routes
        /// </summary>
        /// <param name="calling"></param>
        public static void Register(Assembly calling)
        {
            Type[] theList = calling.GetTypes();
            List<Type> instances = new List<Type>();
            List<Type> routes = new List<Type>();
            foreach (Type theType in theList)
            {
                if(theType.Namespace != null)
                {
                    if (theType.GetInterfaces().Contains(typeof(IWebSocketInstance)))
                    {
                        instances.Add(theType);
                    }
                    else if(theType.GetInterfaces().Contains(typeof(IWebSocketReceiver)))
                    {
                        routes.Add(theType);
                    }
                }
            }
            Register(instances, routes);
        }

        public static void Register(IEnumerable<Type> instances, IEnumerable<Type> routes)
        {
            foreach (Type instanceType in instances)
            {
                if (instanceType.Name.StartsWith("<"))
                {
                    continue;
                }
                MethodInfo? GetInstance = instanceType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (GetInstance == null)
                {
                    continue;
                }
                IWebSocketInstance? instance = (IWebSocketInstance?)GetInstance.Invoke(null, null);
                if (instance == null)
                {
                    continue;
                }
                Console.WriteLine("add ws router " + instance.GetSocketName());
                routers.Add(instance.GetSocketName(), instance);
            }

            foreach (Type routeType in routes)
            {
                MethodInfo? GetInstance = routeType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (GetInstance == null)
                {
                    continue;
                }
                IWebSocketReceiver? instance = (IWebSocketReceiver?)GetInstance.Invoke(null, null);
                if (instance == null)
                {
                    continue;
                }
                Console.WriteLine("Register receiver" + instance.GetType().Name + " => " + instance.DefineTrigger());
                instance.Init();
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


        /// <summary>
        /// Middleware function
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async static Task OnRequest(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.ToString().StartsWith("/ws"))
            {

                if (context.WebSockets.IsWebSocketRequest)
                {
                    string newPath = context.Request.Path.ToString().Replace("/ws", "");
                    newPath = newPath.ToLower();
                    if (routers.ContainsKey(newPath))
                    {
                        System.Net.WebSockets.WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await routers[newPath].StartNewInstance(context, webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        if (enableError)
                        {
                            Console.WriteLine("no router found for " + newPath);
                            string listRouter = string.Join(", ", routers.Keys.ToList());
                            Console.WriteLine("List " + listRouter);
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next();
            }
        }
    }
}
