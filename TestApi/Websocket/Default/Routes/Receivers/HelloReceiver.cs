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
        public override void defineAnswers()
        {
            this.setAnswer<HelloSender>();
        }

        public override string defineTrigger()
        {
            return "/hello";
        }

        public override void defineWebSockets()
        {
            setWebSocket<DefaultSocket>();
        }

        public override async Task<IWebSocketSender> onMessage(WebSocketData socketData, EmptyBody message, WebSocketAnswerOptions options)
        {
            return new HelloSender();
        }
    }
}
