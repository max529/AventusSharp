//using AventusSharp.Data;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace AventusSharp.WebSocket.precast.CRUD.Sender
//{
//    public class GetAllResponse<T> : WebSocketSender<GetAllResponse<T>, List<T>> where T : IStorableWs
//    {
//        public GetAllResponse() { }
//        public GetAllResponse(List<T> result)
//        {
//            body = result;
//        }
//        public override string defineName()
//        {
//            return "/get/all";
//        }
//    }
//}
