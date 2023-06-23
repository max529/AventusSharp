using AventusSharp.WebSocket;

namespace TestApi.Websocket.Default.Routes.Senders
{
    public class LoginResponse : WebSocketSender<LoginResponse, LoginResponse.Body>
    {
        public override string defineName()
        {
            return "/login/response";
        }
        public LoginResponse() { }
        public LoginResponse(bool success)
        {
            this.body.success = success;
        }

        public class Body
        {
            public bool success { get; set; }
        }
    }
}
