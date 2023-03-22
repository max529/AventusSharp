using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    /// <summary>
    /// Instance of websocket define by the name of the class
    /// </summary>
    /// <typeparam name="T">Type of class</typeparam>
    public abstract class WebSocketInstance<T> : IWebSocketInstance where T : IWebSocketInstance, new()
    {
        private Dictionary<string, Func<WebSocketData, Task>> routes = new Dictionary<string, Func<WebSocketData, Task>>();
        private List<Func<WebSocketData, Task>> middlewares;
        private List<WebSocketConnection> connections;
        private static WriteTypeJsonConverter converter;
        private static Dictionary<Type, IWebSocketInstance> instances = new Dictionary<Type, IWebSocketInstance>();
       
        /// <summary>
        /// Default constructor
        /// </summary>
        protected WebSocketInstance()
        {
            middlewares = new List<Func<WebSocketData, Task>>();
            connections = new List<WebSocketConnection>();
            converter = new WriteTypeJsonConverter();
        }
        /// <summary>
        /// Get instance of socket
        /// </summary>
        /// <returns></returns>
        public static T getInstance()
        {
            Type type = typeof(T);
            if (!instances.ContainsKey(type))
            {
                instances[type] = new T();
            }
            if (instances[type] is T casted)
            {
                return casted;
            }
            return default(T);
        }

        /// <summary>
        /// Define socket name like ws://ip:port/ws/${socketName}
        /// You must only return ${socketName}
        /// </summary>
        /// <returns></returns>
        public abstract string getSocketName();

        /// <summary>
        /// Add action when a request go though this WS instance
        /// </summary>
        /// <param name="action"></param>
        public void addUse(Func<WebSocketData, Task> action)
        {
            middlewares.Add(action);
        }
        /// <summary>
        /// Add route inside the ws instance
        /// </summary>
        /// <param name="route"></param>
        public void addRoute(string canal, Func<WebSocketData, Task> action)
        {
            if (routes.ContainsKey(canal))
            {
                routes[canal] = action;
            }
            else
            {
                routes.Add(canal, action);
            }
        }

        /// <summary>
        /// define if the connection can be open
        /// exemple if authentification needed, return false if not login
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public virtual bool canOpenConnection(HttpContext context, System.Net.WebSockets.WebSocket webSocket)
        {
            return true;
        }
        /// <summary>
        /// Start a new connection WS between server and client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        public async Task startNewInstance(HttpContext context, System.Net.WebSockets.WebSocket webSocket)
        {
            if (canOpenConnection(context, webSocket))
            {
                WebSocketConnection connection = new WebSocketConnection(context, webSocket, this);
                connections.Add(connection);
                nbConnectionChanged(connections.Count);
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
        public void removeInstance(WebSocketConnection connection)
        {
            //Log.getInstance().WriteLine("Closing router for " + connection.getContext().Session.Id, "closingSocket");
            try
            {
                if (connections.Contains(connection))
                {
                    connections.Remove(connection);
                    nbConnectionChanged(connections.Count);
                }
            }
            catch
            {

            }
            try
            {
                connection.getWebSocket().Dispose();
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
        public async Task route(WebSocketConnection connection, string channel, JObject data, string uid = "")
        {
            WebSocketData wsData = new WebSocketData(this, connection, data, uid);
            foreach (var middleware in middlewares)
            {

                await middleware(wsData);
            }
            if (routes.ContainsKey(channel))
            {
                try
                {
                    _ = routes[channel].Invoke(wsData);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Number of connection changed
        /// </summary>
        /// <param name="nbConnection"></param>
        protected virtual void nbConnectionChanged(int nbConnection) { }

        #region Broadcast

        /// <summary>
        /// Dispatch a message to all active connections
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="o"></param>
        /// <param name="connectionsToAvoid"></param>
        /// <param name="idsUser"></param>
        /// <returns></returns>
        private List<WebSocketConnection> _broadcast(string eventName, JObject o, List<WebSocketConnection> connectionsToAvoid = null)
        {
            List<WebSocketConnection> emitted = new List<WebSocketConnection>();
            try
            {
                for (int i = 0; i < connections.Count(); i++)
                {
                    WebSocketConnection conn = connections.ElementAt(i);
                    if (conn.getWebSocket().State != System.Net.WebSockets.WebSocketState.Open)
                    {
                        removeInstance(conn);
                        i--;
                    }
                    else
                    {
                        if (connectionsToAvoid == null || !connectionsToAvoid.Contains(conn))
                        {
                             _ = conn.send(eventName, o);
                             emitted.Add(conn);
                        }
                    }
                }
            }
            catch (Exception e)
            {
               Console.WriteLine(e);
            }
            return emitted;
        }

        #region instance

        public List<WebSocketConnection> broadcast(string eventName, object obj, List<WebSocketConnection> connectionToAvoid = null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, converter);
                JObject jObject = JObject.Parse(json);
                return _broadcast(eventName, jObject, connectionToAvoid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return new List<WebSocketConnection>();

            
        }
        public List<WebSocketConnection> broadcast(string eventName, string keyName, object obj, List<WebSocketConnection> connectionToAvoid = null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, converter);
                JToken jTok = JToken.Parse(json);
                JObject jObject = new JObject();
                jObject.Add(keyName, jTok);
                return _broadcast(eventName, jObject, connectionToAvoid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return new List<WebSocketConnection>();
        }
        #endregion
        #endregion
    }
}
