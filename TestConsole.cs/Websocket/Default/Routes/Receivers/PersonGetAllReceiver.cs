using AventusSharp.WebSocket;
using AventusSharp.WebSocket.precast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.cs.Data;
using TestConsole.cs.Websocket.Default.Routes.Senders;

namespace TestConsole.cs.Websocket.Default.Routes.Receivers
{
    class PersonGetAllReceiver : WebSocketReceiverAnswer<PersonGetAllReceiver, EmptyBody>
    {
        public override void DefineAnswers()
        {
            SetAnswer<PersonGetAllSender>();
        }

        public override string DefineTrigger()
        {
            return "/person/get";
        }

        public override void DefineWebSockets()
        {
            SetWebSocket<DefaultSocket>();
        }

        public override async Task<IWebSocketSender> OnMessage(WebSocketData socketData, EmptyBody message, WebSocketAnswerOptions options)
        {
            List<PersonHuman> people = PersonHuman.GetAll();
            return new PersonGetAllSender(people);
        }
    }
}
