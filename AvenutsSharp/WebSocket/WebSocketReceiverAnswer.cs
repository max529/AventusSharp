using AventusSharp.WebSocket.precast;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWebSocketReceiverAnswer : IWebSocketReceiver
    {
        void DefineAnswers();
        List<IWebSocketSender> GetAnswers();
    }
    public class WebSocketAnswerOptions
    {

    }
    public abstract class WebSocketReceiverAnswer<T, U> : WebSocketReceiver<T, U>, IWebSocketReceiverAnswer where T : IWebSocketReceiverAnswer, new() where U : new()
    {
        private readonly List<IWebSocketSender> answers = new();

        public override void Init()
        {
            DefineAnswers();
            base.Init();
        }
        public List<IWebSocketSender> GetAnswers()
        {
            return answers;
        }
        public abstract void DefineAnswers();
        public void SetAnswer<X>() where X : IWebSocketSender
        {
            X? temp = (X?)Activator.CreateInstance(typeof(X));
            if (temp is IWebSocketSenderPrecast precast)
            {
                precast.SetBaseChannel(DefineTrigger());
            }
            if (temp != null)
            {
                answers.Add(temp);
            }
        }

        public sealed async override Task OnMessage(WebSocketData socketData, U message)
        {
            WebSocketAnswerOptions options = new();
            IWebSocketSender answer = await OnMessage(socketData, message, options);
            await answer.Send(socketData.Socket, socketData.Uid);
        }
        public abstract Task<IWebSocketSender> OnMessage(WebSocketData socketData, U message, WebSocketAnswerOptions options);
    }
}
