using AventusSharp.Tools.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket.Event
{
    [NoTypescript]
    public class JsonEvent : WebSocketEvent
    {
        private object o;

        public JsonEvent(object o)
        {
            this.o = o;
        }

        public override async Task Emit()
        {
            if (eventType == WebSocketEventType.Response)
            {
                if(connection == null)
                {
                    throw new WsError(WsErrorCode.NoConnection, "You must provide a connection").GetException();
                }
                await connection.Send(path, o, uid);
            }
            else if (eventType == WebSocketEventType.Others && connection != null)
            {
                await endPoint.Broadcast(path, o, uid, new List<WebSocketConnection>() { connection });
            }
            else
            {
                await endPoint.Broadcast(path, o, uid);
            }
        }
    }
}
