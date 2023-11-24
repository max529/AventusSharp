using AventusSharp.Tools.Attributes;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket.Event
{
    public interface IWebSocketEvent
    {

    }
    [NoTypescript]
    public abstract class WebSocketEvent : IWebSocketEvent
    {
        protected WsEndPoint? endPoint { get; set; }
        protected WebSocketConnection? connection { get; set; }
        protected string uid { get; private set; } = "";
        protected string path { get; set; } = "";
        protected WebSocketEventType eventType { get; set; }


        public WebSocketEvent()
        {

        }

        public void Configure(string path, WsEndPoint endPoint, WebSocketConnection connection, string uid, WebSocketEventType eventType)
        {
            this.eventType = eventType;
            this.endPoint = endPoint;
            this.connection = connection;
            this.uid = uid;
            if (this.path == "")
            {
                this.path = path;
            }
        }

        public Task EmitTo(WebSocketConnection connection)
        {
            this.connection = connection;
            return Emit();
        }



        public abstract Task Emit();
    }
}
