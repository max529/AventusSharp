using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    /// <summary>
    /// Data send when a message a received
    /// </summary>
    public class WebSocketData
    {
        public IWebSocketInstance instance { get; set; }
        /// <summary>
        /// Socket
        /// </summary>
        public WebSocketConnection socket { get; set; }
        /// <summary>
        /// Data recieved in message
        /// </summary>
        public JObject data { get; set; }
        /// <summary>
        /// Metadata added in process
        /// </summary>
        public Dictionary<string, object> metadata { get; set; }
        /// <summary>
        /// Unique identifier of the message
        /// </summary>
        public string uid { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <param name="uid"></param>
        public WebSocketData(IWebSocketInstance instance, WebSocketConnection socket, JObject data, string uid)
        {
            this.instance = instance;
            this.socket = socket;
            this.data = data;
            this.uid = uid;
            this.metadata = new Dictionary<string, object>();
        }

        #region parse data
        /// <summary>
        /// Transform data into object T. Add path to tell where to find data to cast
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propPath">Path where to find data</param>
        /// <returns></returns>
        public T? getData<T>(string propPath = "")
        {
            try
            {
                JToken? dataToUse = data;
                string[] props = propPath.Split(".");
                foreach (string prop in props)
                {
                    if (!string.IsNullOrEmpty(prop))
                    {
                        dataToUse = dataToUse[prop];
                        if (dataToUse == null)
                        {
                            Console.WriteLine("Can't find path " + propPath + " in your data");
                            return default(T);
                        }
                    }
                }
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dataToUse), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects, });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return default(T);
        }

        #endregion

  
    }
}
