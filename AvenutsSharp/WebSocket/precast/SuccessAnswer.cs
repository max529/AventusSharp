using System;
using System.Collections.Generic;
using System.Text;

namespace AventusSharp.WebSocket.precast
{
    public class SuccessAnswer : WebSocketSender<SuccessAnswer, SuccessAnswer.Body>, IWebSocketSenderPrecast
    {
        private string channel;
        public SuccessAnswer() { }
        public SuccessAnswer(string channel)
        {
            this.setBaseChannel(channel);
        }
        public override string defineName()
        {
            return channel;
        }
        public void setBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
        }

    }
    public class SuccessAnswer<T> : WebSocketSender<SuccessAnswer<T>, SuccessAnswer<T>.Body>, IWebSocketSenderPrecast
    {
        private string channel;
        public SuccessAnswer() { }
        public SuccessAnswer(string channel, T data)
        {
            this.setBaseChannel(channel);
            this.body.data = data;
        }
        public override string defineName()
        {
            return channel;
        }
        public void setBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
            public T data;
        }

    }
    public class SuccessAnswerList<T> : WebSocketSender<SuccessAnswerList<T>, SuccessAnswerList<T>.Body>, IWebSocketSenderPrecast
    {
        private string channel;
        public SuccessAnswerList() { }
        public SuccessAnswerList(string channel, List<T> data)
        {
            this.setBaseChannel(channel);
            this.body.data = data;
        }
        public override string defineName()
        {
            return channel;
        }
        public void setBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
            public List<T> data = new List<T>();
        }

    }
    public class SuccessAnswerDico<T> : WebSocketSender<SuccessAnswerDico<T>, SuccessAnswerDico<T>.Body>, IWebSocketSenderPrecast
    {
        private string channel;
        public SuccessAnswerDico() { }
        public SuccessAnswerDico(string channel, Dictionary<int, T> data)
        {
            this.setBaseChannel(channel);
            this.body.data = data;
        }
        public override string defineName()
        {
            return channel;
        }
        public void setBaseChannel(string channel)
        {
            this.channel = channel + "/success";
        }
        public class Body
        {
            public Dictionary<int, T> data = new Dictionary<int, T>();
        }

    }
}
