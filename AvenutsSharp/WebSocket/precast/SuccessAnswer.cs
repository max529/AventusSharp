using System;
using System.Collections.Generic;
using System.Text;

namespace AventusSharp.WebSocket.precast
{
    public class SuccessAnswer : WebSocketSender<SuccessAnswer, SuccessAnswer.Body>, IWebSocketSenderPrecast
    {
        private string channel = "";
        public SuccessAnswer() { }
        public SuccessAnswer(string channel)
        {
            this.SetBaseChannel(channel);
        }
        public override string DefineName()
        {
            return channel;
        }
        public void SetBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
        }

    }
    public class SuccessAnswer<T> : WebSocketSender<SuccessAnswer<T>, SuccessAnswer<T>.Body>, IWebSocketSenderPrecast where T : notnull
    {
        private string channel = "";
        public SuccessAnswer() { }
        public SuccessAnswer(string channel, T data)
        {
            this.SetBaseChannel(channel);
            this.body.data = data;
        }
        public override string DefineName()
        {
            return channel;
        }
        public void SetBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
            public T? data;
        }

    }
    public class SuccessAnswerList<T> : WebSocketSender<SuccessAnswerList<T>, SuccessAnswerList<T>.Body>, IWebSocketSenderPrecast
    {
        private string channel = "";
        public SuccessAnswerList() { }
        public SuccessAnswerList(string channel, List<T> data)
        {
            this.SetBaseChannel(channel);
            this.body.data = data;
        }
        public override string DefineName()
        {
            return channel;
        }
        public void SetBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
            public List<T> data = new();
        }

    }
    public class SuccessAnswerDico<T> : WebSocketSender<SuccessAnswerDico<T>, SuccessAnswerDico<T>.Body>, IWebSocketSenderPrecast
    {
        private string channel = "";
        public SuccessAnswerDico() { }
        public SuccessAnswerDico(string channel, Dictionary<int, T> data)
        {
            this.SetBaseChannel(channel);
            this.body.data = data;
        }
        public override string DefineName()
        {
            return channel;
        }
        public void SetBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
            public Dictionary<int, T> data = new();
        }

    }
}
