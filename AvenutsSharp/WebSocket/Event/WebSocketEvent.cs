using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Attributes;
using System;
using System.Collections.Generic;
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
        protected ResponseTypeEnum eventType { get; set; }

        protected Func<WsEndPoint, WebSocketConnection?, List<WebSocketConnection>>? CustomFct; 


        public WebSocketEvent()
        {

        }

        public void Configure(string path, WsEndPoint endPoint, WebSocketConnection connection, string uid, ResponseTypeEnum eventType)
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

        public void Configure(string path, WsEndPoint endPoint, WebSocketConnection connection, string uid, ResponseTypeEnum eventType, Func<WsEndPoint, WebSocketConnection?, List<WebSocketConnection>>? customFct)
        {
            Configure(path, endPoint, connection, uid, eventType);
            CustomFct = customFct;
        }

        public Task EmitTo(WebSocketConnection connection)
        {
            this.connection = connection;
            return Emit();
        }



        public abstract Task Emit();

        protected async Task DefaultEmit(object? o)
        {
            if (eventType == ResponseTypeEnum.Single)
            {
                if (connection == null)
                {
                    throw new WsError(WsErrorCode.NoConnection, "You must provide a connection").GetException();
                }
                await connection.Send(path, o, uid);
            }
            else if (endPoint != null)
            {
                if (eventType == ResponseTypeEnum.Custom && CustomFct != null)
                {
                    await endPoint.Broadcast(path, o, uid, CustomFct(endPoint, connection));
                }
                else if (eventType == ResponseTypeEnum.Others && connection != null)
                {
                    await endPoint.Broadcast(path, o, uid, null, new List<WebSocketConnection>() { connection });
                }
                else
                {
                    await endPoint.Broadcast(path, o, uid);
                }
            }
        }
    }
}
