using AventusSharp.WebSocket;

namespace TestApi.Websocket.Default.Routes.Senders
{
    public class HelloSender : WebSocketSender<HelloSender, HelloSender.Body>
    {
        public override string defineName()
        {
            return "/hello/response";
        }

        public class Body
        {
            public string msg { get; set; } = "Hello";
        }
    }
}
