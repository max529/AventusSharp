using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket.Event
{
    [NoTypescript]
    public class EmptyEvent : WebSocketEvent
    {
        public override async Task Emit()
        {
            path = basePath;
            await DefaultEmit(null);
        }
    }
}
