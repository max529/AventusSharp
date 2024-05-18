using AventusSharp.Tools;
using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
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
        protected string basePath { get; set; } = "";
        protected string? path { get; set; } = null;
        protected ResponseTypeEnum eventType { get; set; }

        protected Func<WsEndPoint, WebSocketConnection?, List<WebSocketConnection>>? CustomFct;


        public WebSocketEvent()
        {

        }

        public void Configure(string basePath, WsEndPoint endPoint, WebSocketConnection connection, string uid, ResponseTypeEnum eventType)
        {
            this.eventType = eventType;
            this.endPoint = endPoint;
            this.connection = connection;
            this.uid = uid;
            if (this.basePath == "")
            {
                this.basePath = basePath;
            }
        }

        public void Configure(string basePath, WsEndPoint endPoint, WebSocketConnection connection, string uid, ResponseTypeEnum eventType, Func<WsEndPoint, WebSocketConnection?, List<WebSocketConnection>>? customFct)
        {
            Configure(basePath, endPoint, connection, uid, eventType);
            CustomFct = customFct;
        }

        public Task EmitTo(WebSocketConnection connection, string uid = "")
        {
            this.uid = uid;
            this.eventType = ResponseTypeEnum.Single;
            this.connection = connection;
            return Emit();
        }
        public Task EmitTo(WsEndPoint endPoint, string uid = "")
        {
            this.uid = uid;
            eventType = ResponseTypeEnum.Broadcast;
            this.endPoint = endPoint;
            return Emit();
        }

        public async Task<VoidWithError> EmitTo<T>(string uid = "") where T : WsEndPoint
        {
            VoidWithError result = new VoidWithError();
            WsEndPoint? endPoint = WebSocketMiddleware.endPointInstances.Values.FirstOrDefault(p => p.GetType() == typeof(T));
            if (endPoint == null)
            {
                result.Errors.Add(new WsError(WsErrorCode.NoEndPoint, "No endpoint of type " + typeof(T).Name + " found. Did you register the WebSocketMiddleware?"));
                return result;
            }
            this.uid = uid;
            eventType = ResponseTypeEnum.Broadcast;
            this.endPoint = endPoint;
            await Emit();
            return result;
        }


        public abstract Task Emit();

        protected async Task DefaultEmit(object? o)
        {
            if (path == null)
            {
                throw new WsError(WsErrorCode.NoPath, "The path isn't transformed from basePath " + basePath).GetException();
            }
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
            else
            {
                throw new WsError(WsErrorCode.NoEndPoint, "You must provide a endpoint. Maybe you can use the function EmitTo()").GetException();
            }
        }
    }
}
