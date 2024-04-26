using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Attributes;
using AventusSharp.WebSocket.Event;
using System;
using System.Collections.Generic;

namespace AventusSharp.WebSocket
{
    public interface IWsRoute
    {
        void AddEndPoint(WsEndPoint endPoint);
    }
    [NoTypescript]
    public abstract class WsRoute : IWsRoute
    {
        protected List<WsEndPoint> Endpoints { get; set; } = new List<WsEndPoint>();

        [NotRoute]
        public void AddEndPoint(WsEndPoint endPoint)
        {
            if (!Endpoints.Contains(endPoint))
            {
                Endpoints.Add(endPoint);
            }
        }

        protected void TriggerEvent(WebSocketEvent @event)
        {
            foreach (WsEndPoint endPoint in Endpoints)
            {
                @event.EmitTo(endPoint);
            }
        }
    }
}
