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
        public static bool enableError = false;
        private static Dictionary<string, IWebSocketInstance> routers = new Dictionary<string, IWebSocketInstance>();
        /// <summary>
        /// Create all Websocket Instance inside current Assembly + load all routes inside namespace $CURRENT.Routes
        /// </summary>
        /// <param name="calling"></param>
        public static void register(Assembly calling)
        {
            Type[] theList = calling.GetTypes();

            IEnumerable<Type> instances = theList.Where(type => type.Namespace != null && type.GetInterfaces().Contains(typeof(IWebSocketInstance)));

            foreach (Type instanceType in instances)
            {
                if (instanceType.Name.StartsWith("<"))
                {
                    continue;
                }
                MethodInfo? getInstance = instanceType.GetMethod("getInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (getInstance == null)
                {
                    continue;
                }
                IWebSocketInstance? instance = (IWebSocketInstance?)getInstance.Invoke(null, null);
                if (instance == null)
                {
                    continue;
                }
                Console.WriteLine("add ws router " + instance.getSocketName());
                routers.Add(instance.getSocketName(), instance);
            }

            IEnumerable<Type> routes = theList.Where(type => type.Namespace != null && type.GetInterfaces().Contains(typeof(IWebSocketReceiver)));

            foreach (Type routeType in routes)
            {
                MethodInfo? getInstance = routeType.GetMethod("getInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (getInstance == null)
                {
                    continue;
                }
                IWebSocketReceiver? instance = (IWebSocketReceiver?)getInstance.Invoke(null, null);
                if (instance == null)
                {
                    continue;
                }
                Console.WriteLine("Register receiver" + instance.GetType().Name + " => " + instance.defineTrigger());
                instance.init();
            }
        }

        public static void register()
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                register(entry);
            }
        }


        /// <summary>
        /// Middleware function
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async static Task onRequest(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path.ToString().StartsWith("/ws"))
            {

                if (context.WebSockets.IsWebSocketRequest)
                {
                    string newPath = context.Request.Path.ToString().Replace("/ws", "");
                    newPath = newPath.ToLower();
                    newPath = newPath.Replace("/", "");
                    if (routers.ContainsKey(newPath))
                    {
                        System.Net.WebSockets.WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await routers[newPath].startNewInstance(context, webSocket);
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
