using AventusSharp.WebSocket.precast;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWebSocketReceiverAnswer : IWebSocketReceiver
    {
        void defineAnswers();
        List<IWebSocketSender> getAnswers();
    }
    public class WebSocketAnswerOptions
    {

    }
    public abstract class WebSocketReceiverAnswer<T, U> : WebSocketReceiver<T, U>, IWebSocketReceiverAnswer where T : IWebSocketReceiverAnswer, new() where U : new()
    {
        private List<IWebSocketSender> answers = new List<IWebSocketSender>();

        public override void init()
        {
            defineAnswers();
            base.init();
        }
        public List<IWebSocketSender> getAnswers()
        {
            return answers;
        }
        public abstract void defineAnswers();
        public void setAnswer<X>() where X : IWebSocketSender
        {
            X? temp = (X?)Activator.CreateInstance(typeof(X));
            if (temp is IWebSocketSenderPrecast precast)
            {
                precast.setBaseChannel(defineTrigger());
            }
            if (temp != null)
            {
                answers.Add(temp);
            }
        }

        public sealed async override Task onMessage(WebSocketData socketData, U message)
        {
            WebSocketAnswerOptions options = new WebSocketAnswerOptions();
            IWebSocketSender answer = await onMessage(socketData, message, options);
            await answer.send(socketData.socket, socketData.uid);
        }
        public abstract Task<IWebSocketSender> onMessage(WebSocketData socketData, U message, WebSocketAnswerOptions options);
    }
}
