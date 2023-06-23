using AventusSharp.WebSocket;
using TestApi.Websocket.Default.Routes.Senders;

namespace TestApi.Websocket.Default.Routes.Receivers
{
    public class Login : WebSocketReceiverAnswer<Login, Login.Body>
    {

        public override string defineTrigger()
        {
            return "/login";
        }

        public override void defineWebSockets()
        {
            setWebSocket<DefaultSocket>();
        }
        public override void defineAnswers()
        {
            setAnswer<LoginResponse>();
        }

        public override async Task<IWebSocketSender> onMessage(WebSocketData socketData, Body message, WebSocketAnswerOptions options)
        {
            if (message.username == "root" && message.password == "root")
            {
                return new LoginResponse(success: true);
            }
            return new LoginResponse(success: false);
        }

        public class Body
        {
            public string username { get; set; }
            public string password { get; set; }
        }
    }
}
