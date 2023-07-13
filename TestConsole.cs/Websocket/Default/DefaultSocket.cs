using AventusSharp.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.cs.Websocket.Default
{
    public class DefaultSocket : WebSocketInstance<DefaultSocket>
    {
        public override string GetSocketName()
        {
            return "/";
        }
    }
}
