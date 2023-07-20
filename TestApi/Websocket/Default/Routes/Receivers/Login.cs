using AventusSharp.WebSocket;
using TestApi.Websocket.Default.Routes.Senders;

namespace TestApi.Websocket.Default.Routes.Receivers
{
    public class Login : WebSocketReceiverAnswer<Login, Login.Body>
    {

        public override string DefineTrigger()
        {
            return "/login";
        }

        public override void DefineWebSockets()
        {
            SetWebSocket<DefaultSocket>();
        }
        public override void DefineAnswers()
        {
            SetAnswer<LoginResponse>();
        }

        public override async Task<IWebSocketSender> OnMessage(WebSocketData socketData, Body message, WebSocketAnswerOptions options)
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
