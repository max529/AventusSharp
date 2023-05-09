//using AventusSharp.Data;
//using AventusSharp.Tools;
//using AventusSharp.WebSocket.precast.CRUD.Sender;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace AventusSharp.WebSocket.precast.CRUD.Receiver
//{
//    public class GetAll<T> : WebSocketReceiverAnswer<GetAll<T>, EmptyBody> where T : IStorableWs, new()
//    {
//        private T instance;
//        public GetAll()
//        {
//            instance = TypeTools.CreateNewObj<T>();
            
//        }
//        public override void defineAnswers()
//        {
//            setAnswer<GetAllResponse<T>>();
//        }

//        public override string defineTrigger()
//        {
//            throw new NotImplementedException();
//        }

//        public override void defineWebSockets()
//        {
//           instance.DefineWebSockets();
//        }

//        public override async Task<IWebSocketSender> onMessage(WebSocketData socketData, EmptyBody message, WebSocketAnswerOptions options)
//        {
//            return new GetAllResponse<T>(Storable<T>.GetAll());
//        }
//    }
//}
