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
        public IWebSocketInstance Instance { get; set; }
        /// <summary>
        /// Socket
        /// </summary>
        public WebSocketConnection Socket { get; set; }
        /// <summary>
        /// Data recieved in message
        /// </summary>
        public JObject Data { get; set; }
        /// <summary>
        /// Metadata added in process
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }
        /// <summary>
        /// Unique identifier of the message
        /// </summary>
        public string Uid { get; set; }
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        /// <param name="uid"></param>
        public WebSocketData(IWebSocketInstance instance, WebSocketConnection socket, JObject data, string uid)
        {
            Instance = instance;
            Socket = socket;
            Data = data;
            Uid = uid;
            Metadata = new Dictionary<string, object>();
        }

        #region parse data
        /// <summary>
        /// Transform data into object T. Add path to tell where to find data to cast
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propPath">Path where to find data</param>
        /// <returns></returns>
        public T? GetData<T>(string propPath = "")
        {
            try
            {
                JToken? dataToUse = Data;
                string[] props = propPath.Split(".");
                foreach (string prop in props)
                {
                    if (!string.IsNullOrEmpty(prop))
                    {
                        dataToUse = dataToUse[prop];
                        if (dataToUse == null)
                        {
                            Console.WriteLine("Can't find path " + propPath + " in your data");
                            return default;
                        }
                    }
                }
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(dataToUse), new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects, });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return default;
        }

        #endregion

  
    }
}
