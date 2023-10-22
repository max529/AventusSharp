using System.Collections.Generic;

namespace AventusSharp.WebSocket
{
    internal class WebSocketAttributeAnalyze
    {
        public List<string> pathes = new List<string>();
        public List<WsEndPoint> endPoints = new List<WsEndPoint>();
        public WebSocketEventType eventType = WebSocketEventType.Response;
    }
}
