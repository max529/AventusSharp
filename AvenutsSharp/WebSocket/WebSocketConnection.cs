using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    /// <summary>
    /// Class that represent a connection Ws between Client and Server
    /// </summary>
    public class WebSocketConnection
    {
        private static readonly char fileDelimiter = '°';
        public static bool DisplayMsg { get; set; }
        private readonly HttpContext context;
        private readonly System.Net.WebSockets.WebSocket webSocket;
        private readonly IWebSocketInstance instance;
        private WebSocketReceiveResult? result;
        private readonly WriteTypeJsonConverter converter;
        private readonly Dictionary<string, FileBodyElement> filesInProgress = new();

        /// <summary>
        /// get context of the request
        /// </summary>
        /// <returns></returns>
        public HttpContext GetContext()
        {
            return context;
        }
        /// <summary>
        /// get WebSocket class from c#
        /// </summary>
        /// <returns></returns>
        public System.Net.WebSockets.WebSocket GetWebSocket()
        {
            return webSocket;
        }
        /// <summary>
        /// default constructor
        /// </summary>
        /// <param name="context">context of request HTTP</param>
        /// <param name="webSocket">websocket create</param>
        /// <param name="instance">Instance of WebsocketInstance (parent)</param>
        /// <param name="ownerId">Owner id</param>
        public WebSocketConnection(HttpContext context, System.Net.WebSockets.WebSocket webSocket, IWebSocketInstance instance)
        {
            this.context = context;
            this.webSocket = webSocket;
            this.instance = instance;
            converter = new WriteTypeJsonConverter();
        }
        /// <summary>
        /// Start the WebSocket connection
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            byte[] buffer = new byte[1024 * 4];
            bool websocketHasError = false;
            try
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                string msg = "";

                while (!result.CloseStatus.HasValue)
                {
                    if (buffer[0] == fileDelimiter)
                    {
                        byte[] uidBuff = buffer.Take(10).ToArray();
                        byte[] lastBuff = buffer.Skip(buffer.Length - 12).Take(11).ToArray();
                        string uid = Encoding.UTF8.GetString(uidBuff);
                        string lastPart = Encoding.UTF8.GetString(lastBuff);
                        if (lastPart == uid + fileDelimiter)
                        {
                            // end
                            filesInProgress[uid].Stream.Write(buffer, 10, buffer.Length - 21);
                            filesInProgress[uid].Stream.Close();
                            filesInProgress[uid].Stream.Dispose();
                            filesInProgress.Remove(uid);
                            JObject obj = new()
                            {
                                ["uid"] = "uid"
                            };
                            _ = this.Send("/file_uploaded", (object)obj);
                        }
                        else
                        {
                            filesInProgress[uid].Stream.Write(buffer, 10, buffer.Length - 10);
                        }

                    }
                    else
                    {
                        websocketHasError = false;
                        byte[] buffTemp = new byte[result.Count];
                        for (int i = 0; i < buffTemp.Length; i++)
                        {
                            buffTemp[i] = buffer[i];
                        }
                        msg += Encoding.UTF8.GetString(buffTemp);
                        if (result.EndOfMessage)
                        {
                            try
                            {

                                JObject o = JObject.Parse(msg);
                                if (o["channel"]?.ToString() == "ping")
                                {
                                    _ = Send("pong", (object)new JObject());
                                }
                                else if (o["channel"]?.ToString() == "/register_file_upload")
                                {
                                    JObject data = new();
                                    string? dataObjectString = o["data"]?.ToString();
                                    string? filename = o["filename"]?.ToString();
                                    string? uid = o["uid"]?.ToString();
                                    if (dataObjectString == null || filename == null || uid == null)
                                    {
                                        return;
                                    }
                                    data = JObject.Parse(dataObjectString);
                                    // create temp
                                    string tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                                    if (!Directory.Exists(tempFolder))
                                    {
                                        Directory.CreateDirectory(tempFolder);
                                    }
                                    // prepare file
                                    string filePath = Path.Combine(tempFolder, filename);
                                    int i = 0;
                                    while (File.Exists(filePath))
                                    {
                                        List<string> splitted = filename.Split(".").ToList();
                                        splitted.Insert(splitted.Count - 1, i + "");
                                        i++;
                                        filePath = Path.Combine(tempFolder, string.Join(".", splitted));
                                    }
                                    FileBodyElement fileBody = new (filename, new FileStream(filePath, FileMode.Create), filePath);
                                    filesInProgress[uid] = fileBody;
                                    _ = this.Send("/register_file_upload/done", (object)new JObject(), uid);
                                }
                                else
                                {
                                    if (DisplayMsg)
                                    {
                                        Console.WriteLine("on a reçu sur " + o["channel"], "onMessage");
                                    }
                                    string? channel = o["channel"]?.ToString();
                                    if (channel == null)
                                    {
                                        return;
                                    }
                                    JObject data = new();
                                    string? objString = o.ContainsKey("data") ? o["data"]?.ToString() : null;
                                    if (objString != null)
                                    {
                                        data = JObject.Parse(objString);
                                    }
                                    string? uid = o.ContainsKey("uid") ? o["uid"]?.ToString() : null;
                                    if (uid != null)
                                    {
                                        await instance.Route(this, channel, data, uid.ToString());
                                    }
                                    else
                                    {
                                        await instance.Route(this, channel, data);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error on parse message from socket : " + e.Message, "errorParsingMessage");
                            }
                            msg = "";
                        }
                    }
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception e)
            {
                if (!websocketHasError)
                {
                    websocketHasError = true;
                    Console.WriteLine(e);
                }
            }
            instance.RemoveInstance(this);
        }

        #region Send
        /// <summary>
        /// Send a msg though this connection
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="o">Object to send</param>
        /// <param name="uid">Uid to identify request</param>
        /// <returns></returns>
        private async Task Send(string eventName, JObject o, string? uid = null)
        {
            if (o.ContainsKey("$type"))
            {
                o.Remove("$type");
            }
            string data = o.ToString(Newtonsoft.Json.Formatting.None);
            JObject toSend = new()
            {
                { "channel", eventName },
                { "data", data }
            };
            if (!string.IsNullOrEmpty(uid))
            {
                toSend.Add("uid", uid);
            }

            byte[] dataToSend = Encoding.UTF8.GetBytes(toSend.ToString(Newtonsoft.Json.Formatting.None));

            try
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.SendAsync(dataToSend, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else
                {
                    this.instance.RemoveInstance(this);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in RouterSocket.send() : " + e.ToString());
                this.instance.RemoveInstance(this);
            }
        }

        public async Task Send(string eventName, object obj, string? uid = null)
        {
            try
            {
                //obj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(obj, settings), settings);
                string json = JsonConvert.SerializeObject(obj, converter);
                JObject jObject = JObject.Parse(json);
                await Send(eventName, jObject, uid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task Send(string eventName, string keyName, object obj, string? uid = null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj, converter);
                JToken jTok = JToken.Parse(json);
                JObject jObject = new()
                {
                    { keyName, jTok }
                };
                await Send(eventName, jObject, uid);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        #endregion


        private class FileBodyElement
        {
            public string Filename { get; private set; }
            public FileStream Stream { get; private set; }
            public string PathTemp { get; private set; }

            public FileBodyElement(string filename, FileStream stream, string pathTemp)
            {
                Filename = filename;
                Stream = stream;
                PathTemp = pathTemp;
            }
        }
    }


}
