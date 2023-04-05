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
        public override void defineAnswers()
        {
            this.setAnswer<PersonGetAllSender>();
        }

        public override string defineTrigger()
        {
            return "/person/get";
        }

        public override void defineWebSockets()
        {
            setWebSocket<DefaultSocket>();
        }

        public override async Task<IWebSocketSender> onMessage(WebSocketData socketData, EmptyBody message, WebSocketAnswerOptions options)
        {
            List<Person> people = Person.GetAll();
            return new PersonGetAllSender(people);
        }
    }
}
