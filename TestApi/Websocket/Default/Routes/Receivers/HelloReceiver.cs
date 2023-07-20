using AventusSharp.WebSocket;
using AventusSharp.WebSocket.precast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestApi.Websocket.Default.Routes.Senders;

namespace TestApi.Websocket.Default.Routes.Receivers
{
    class HelloReceiver : WebSocketReceiverAnswer<HelloReceiver, EmptyBody>
    {
        public override void DefineAnswers()
        {
            SetAnswer<HelloSender>();
        }

        public override string DefineTrigger()
        {
            return "/hello";
        }

        public override void DefineWebSockets()
        {
            SetWebSocket<DefaultSocket>();
        }

        public override async Task<IWebSocketSender> OnMessage(WebSocketData socketData, EmptyBody message, WebSocketAnswerOptions options)
        {
            return new HelloSender();
        }
    }
}
