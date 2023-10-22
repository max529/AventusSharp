using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Event;
using AventusSharp.WebSocket.Request;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWsEndPoint
    {
        string Path();

        bool Main();
    }
    
    [NoTypescript]
    public abstract class WsEndPoint : IWsEndPoint
    {
       

        internal Dictionary<string, WebSocketRouteInfo> routesInfo = new Dictionary<string, WebSocketRouteInfo>();
        private readonly List<WebSocketConnection> connections = new();
        private readonly List<Func<WebSocketConnection, string, WebSocketRouterBody, string, Task<bool>>> middlewares = new();
        internal JsonConverter converter;
        public WsEndPoint()
        {
            Configure();
            if (converter == null)
            {
                converter = WebSocketMiddleware.config.CustomJSONConverter;
            }
        }
        public abstract string Path();

        public virtual bool Main()
        {
            return false;
        }

        protected virtual void Configure()
        {
        }

        protected void setConverter(JsonConverter converter)
        {
            this.converter = converter;
        }

        /// <summary>
        /// Add action when a request go though this WS instance
        /// </summary>
        /// <param name="action"></param>
        public WsEndPoint Use(Func<WebSocketConnection, string, WebSocketRouterBody, string, Task<bool>> action)
        {
            middlewares.Add(action);
            return this;
        }

        /// <summary>
        /// define if the connection can be open
        /// exemple if authentification needed, return false if not login
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public virtual bool CanOpenConnection(HttpContext context, System.Net.WebSockets.WebSocket webSocket)
        {
            return true;
        }

        /// <summary>
        /// Start a new connection WS between server and client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        internal async Task StartNewInstance(HttpContext context, System.Net.WebSockets.WebSocket webSocket)
        {
            if (CanOpenConnection(context, webSocket))
            {
                WebSocketConnection connection = new(context, webSocket, this);
                connections.Add(connection);
                await connection.Start();
            }
            else
            {
                context.Response.StatusCode = 302;
                webSocket.Abort();
            }
        }
        /// <summary>
        /// Remove connection of WS 
        /// </summary>
        /// <param name="connection"></param>
        public void RemoveInstance(WebSocketConnection connection)
        {
            try
            {
                if (connections.Contains(connection))
                {
                    connections.Remove(connection);
                }
            }
            catch
            {

            }
            try
            {
                connection.GetWebSocket().Dispose();
            }
            catch
            {

            }

        }


        /// <summary>
        /// This function is called to route to request to correct route
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task Route(WebSocketConnection connection, string path, WebSocketRouterBody body, string uid = "")
        {
            foreach (Func<WebSocketConnection, string, WebSocketRouterBody, string, Task<bool>> middleware in middlewares)
            {
                if (!await middleware(connection, path, body, uid))
                {
                    return;
                }
            }
            foreach (KeyValuePair<string, WebSocketRouteInfo> infoTemp in routesInfo)
            {
                WebSocketRouteInfo routeInfo = infoTemp.Value;
                Match match = routeInfo.pattern.Match(path);
                if (match.Success)
                {
                    if (WebSocketMiddleware.config.PrintTrigger)
                        Console.WriteLine("trigger " + routeInfo.ToString());

                    object?[] param = new object[routeInfo.nbParamsFunction];
                    Dictionary<Type, object> wellKnowned = new Dictionary<Type, object>()
                    {
                        {  typeof(WebSocketConnection), connection },
                        {  typeof(WsEndPoint), this },
                        {  typeof(HttpContext), connection.GetContext() },
                        {  typeof(System.Net.WebSockets.WebSocket), connection.GetWebSocket() },
                    };
                    foreach (WebSocketRouterParameterInfo parameter in routeInfo.parameters.Values)
                    {
                        if (parameter.positionCSharp != -1)
                        {
                            if (parameter.positionUrl == -1)
                            {
                                if(wellKnowned.ContainsKey(parameter.type))
                                {
                                    param[parameter.positionCSharp] = wellKnowned[parameter.type];
                                }
                                else
                                {
                                    object? value = null;
                                    // check if dependancies injection
                                    if (WebSocketMiddleware.injected.ContainsKey(parameter.type))
                                    {
                                        value = WebSocketMiddleware.injected[parameter.type];
                                    }
                                    // check if body
                                    else
                                    {
                                        ResultWithWsError<object> bodyPart = body.GetData(parameter.type, parameter.name);
                                        if (!bodyPart.Success)
                                        {
                                            await connection.Send(path, bodyPart);
                                            return;
                                        }
                                        value = bodyPart.Result;
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
                                    param[parameter.positionCSharp] = Routes.Form.Tools.DefaultValue(parameter.type);
                                }

                            }
                        }
                    }
                    WebSocketEvent? _event = null;
                    if (routeInfo.action.ReturnType == typeof(void))
                    {
                        routeInfo.action.Invoke(routeInfo.router, param);
                        _event = new EmptyEvent();
                    }
                    else
                    {
                        object? o = routeInfo.action.Invoke(routeInfo.router, param);
                        if (o is Task task)
                        {
                            task.Wait();
                            if (!o.GetType().IsGenericType)
                            {
                                _event = new EmptyEvent();
                            }
                            else
                            {
                                o = o.GetType().GetProperty("Result")?.GetValue(o);
                            }
                        }
                        if (_event == null)
                        {
                            if (o is WebSocketEvent response)
                            {
                                _event = response;
                            }
                            else
                            {
                                _event = new JsonEvent(o);
                            }
                        }
                        _event.Configure(path, this, connection, uid, routeInfo.eventType);
                        await _event.Emit();
                    }
                }
            }
        }



        /// <summary>
        /// Dispatch a message to all active connections
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="o"></param>
        /// <param name="connectionsToAvoid"></param>
        /// <param name="idsUser"></param>
        /// <returns></returns>
        private async Task Broadcast(string eventName, JObject o, string? uid = null, List<WebSocketConnection>? omit = null)
        {
            try
            {
                string data = o.ToString(Formatting.None);
                JObject toSend = new()
                {
                    { "channel", eventName },
                    { "data", data }
                };
                if (!string.IsNullOrEmpty(uid))
                {
                    toSend.Add("uid", uid);
                }
                byte[] dataToSend = Encoding.UTF8.GetBytes(toSend.ToString(Formatting.None));
                await Broadcast(dataToSend, omit);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // <summary>
        /// Dispatch a message to all active connections
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="o"></param>
        /// <param name="connectionsToAvoid"></param>
        /// <param name="idsUser"></param>
        /// <returns></returns>
        private async Task Broadcast(byte[] dataToSend, List<WebSocketConnection>? omit = null)
        {
            try
            {
                if (omit == null)
                {
                    omit = new();
                }
                for (int i = 0; i < connections.Count; i++)
                {
                    WebSocketConnection conn = connections.ElementAt(i);
                    if (omit.Contains(conn))
                    {
                        continue;
                    }
                    if (conn.GetWebSocket().State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        RemoveInstance(conn);
                        i--;
                    }
                    else
                    {
                        // todo implement parallelism here
                        await conn.Send(dataToSend);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        /// <summary>
        /// Dispatch a message to all active connections
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="o"></param>
        /// <param name="connectionsToAvoid"></param>
        /// <param name="idsUser"></param>
        /// <returns></returns>
        public async Task Broadcast(string eventName, object? obj = null, string? uid = null, List<WebSocketConnection>? omit = null)
        {
            try
            {
                JObject jObject = new JObject();
                if (obj != null)
                {
                    string json = JsonConvert.SerializeObject(obj, converter);
                    jObject = JObject.Parse(json);
                }
                await Broadcast(eventName, jObject, uid, omit);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }

    [NoTypescript]
    public sealed class DefaultWsEndPoint : WsEndPoint
    {
        public override string Path()
        {
            return "/ws";
        }
    }
}
