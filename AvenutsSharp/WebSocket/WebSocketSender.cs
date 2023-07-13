using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWebSocketSender {
        string DefineName();
        Type GetBody();
        Task Send(WebSocketConnection connection, string uid = "");
        void Broadcast(IWebSocketInstance instance, List<WebSocketConnection>? connectionToAvoid = null);
    }
    public abstract class WebSocketSender<T, U> : IWebSocketSender where T : IWebSocketSender, new() where U : notnull, new()
    {
        protected U body = new();
        public abstract string DefineName();
        public Type GetBody()
        {
            return typeof(U);
        }
        public async Task Send(WebSocketConnection connection, string uid = "")
        {
            await connection.Send(DefineName(), body, uid);
        }
        public void Broadcast(IWebSocketInstance instance, List<WebSocketConnection>? connectionToAvoid = null)
        {
            instance.Broadcast(DefineName(), body, connectionToAvoid);
        }

    }
}
