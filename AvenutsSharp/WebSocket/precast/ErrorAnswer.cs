using System;
using System.Collections.Generic;
using System.Text;

namespace AventusSharp.WebSocket.precast
{
    
    public class ErrorAnswer : WebSocketSender<ErrorAnswer, ErrorAnswer.Body>, IWebSocketSenderPrecast
    {
        private string channel = "";
        public ErrorAnswer() { }
        public ErrorAnswer(string channel, string message)
        {
            SetBaseChannel(channel);
            body.message = message;
        }
        public override string DefineName()
        {
            return channel;
        }
        public void SetBaseChannel(string channel)
        {
            this.channel = channel + "/error";
        }

        public class Body
        {
            public string message = "";
        }
    }
}
