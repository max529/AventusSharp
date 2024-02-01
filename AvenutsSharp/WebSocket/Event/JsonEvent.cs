using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket.Event
{
    [NoTypescript]
    public class JsonEvent : WebSocketEvent
    {
        private object? o;

        public JsonEvent(object? o)
        {
            this.o = o;
        }

        public override async Task Emit()
        {
            await DefaultEmit(o);
        }
    }
}
