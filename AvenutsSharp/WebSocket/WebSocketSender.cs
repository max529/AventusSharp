using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWebSocketSender {
        string defineName();
        Type getBody();
        Task send(WebSocketConnection connection, string uid = "");
        void broadcast(IWebSocketInstance instance, List<WebSocketConnection>? connectionToAvoid = null);
    }
    public abstract class WebSocketSender<T, U> : IWebSocketSender where T : IWebSocketSender, new() where U : notnull, new()
    {
        protected U body = new U();
        public abstract string defineName();
        public Type getBody()
        {
            return typeof(U);
        }
        public async Task send(WebSocketConnection connection, string uid = "")
        {
            await connection.send(defineName(), body, uid);
        }
        public void broadcast(IWebSocketInstance instance, List<WebSocketConnection>? connectionToAvoid = null)
        {
            instance.broadcast(defineName(), body, connectionToAvoid);
        }

    }
}
