using AventusSharp.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.cs.Data;

namespace TestConsole.cs.Websocket.Default.Routes.Senders
{
    public class PersonGetAllSender : WebSocketSender<PersonGetAllSender, List<PersonHuman>>
    {
        public PersonGetAllSender()
        {

        }
        public PersonGetAllSender(List<PersonHuman> people)
        {
            body = people;
        }
        public override string DefineName()
        {
            return "/person/get/sucess";
        }
    }
}
