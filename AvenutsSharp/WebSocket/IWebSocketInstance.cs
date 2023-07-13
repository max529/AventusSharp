using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    /// <summary>
    /// Instance of websocket define by the name of the class
    /// </summary>
    public interface IWebSocketInstance
    {

        string GetSocketName();
        /// <summary>
        /// Add route inside the ws instance
        /// </summary>
        /// <param name="route"></param>
        void AddRoute(string canal, Func<WebSocketData, Task> action);
        /// <summary>
        /// Start a new connection WS between server and client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        Task StartNewInstance(HttpContext context, System.Net.WebSockets.WebSocket webSocket);
        /// <summary>
        /// This function is called to route to request to correct route
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="channel"></param>
        /// <param name="data"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        Task Route(WebSocketConnection connection, string channel, JObject data, string uid = "");
        /// <summary>
        /// Remove connection of WS 
        /// </summary>
        /// <param name="connection"></param>
        void RemoveInstance(WebSocketConnection connection);
        /// <summary>
        /// Dispatch a message to all active connections
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="obj"></param>
        /// <param name="connectionToAvoid"></param>
        /// <param name="idsUser"></param>
        /// <returns></returns>
        List<WebSocketConnection> Broadcast(string eventName, object obj, List<WebSocketConnection>? connectionToAvoid = null);
        /// <summary>
        /// Dispatch a message to all active connections
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="obj"></param>
        /// <param name="connectionToAvoid"></param>
        /// <param name="idsUser"></param>
        /// <returns></returns>
        List<WebSocketConnection> Broadcast(string eventName, string keyName, object obj, List<WebSocketConnection>? connectionToAvoid = null);
    }
}
