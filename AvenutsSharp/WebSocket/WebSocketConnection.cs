﻿using AventusSharp.Tools;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
using AventusSharp.WebSocket.Request;
using Scriban.Parsing;

namespace AventusSharp.WebSocket
{
    /// <summary>
    /// Class that represent a connection Ws between Client and Server
    /// </summary>
    public class WebSocketConnection
    {
        public static bool DisplayMsg { get; set; }
        private readonly HttpContext context;
        private readonly System.Net.WebSockets.WebSocket webSocket;
        private readonly WsEndPoint instance;
        private WebSocketReceiveResult? result;

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
        public WebSocketConnection(HttpContext context, System.Net.WebSockets.WebSocket webSocket, WsEndPoint instance)
        {
            this.context = context;
            this.webSocket = webSocket;
            this.instance = instance;
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
                            else
                            {
                                if (DisplayMsg)
                                {
                                    Console.WriteLine("on a reçu sur " + o["channel"], "onMessage");
                                }
                                string? channel = o["channel"]?.ToString().ToLower();
                                if (channel == null)
                                {
                                    return;
                                }

                                string? bodyTxt = o.ContainsKey("data") ? o["data"]?.ToString() : null;
                                WebSocketRouterBody body = new(bodyTxt);

                                string? uid = o.ContainsKey("uid") ? o["uid"]?.ToString() : null;
                                if (uid != null)
                                {
                                    await instance.Route(this, channel, body, uid.ToString());
                                }
                                else
                                {
                                    await instance.Route(this, channel, body);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error on parse message from socket : " + e.Message, "errorParsingMessage");
                        }
                        msg = "";
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
        /// <param name="data">string to send</param>
        /// <param name="uid">Uid to identify request</param>
        /// <returns></returns>
        private async Task Send(string eventName, string data, string? uid = null)
        {
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

            await Send(dataToSend);
        }
        /// <summary>
        /// Send a msg though this connection
        /// </summary>
        /// <param name="eventName">Event name</param>
        /// <param name="o">Object to send</param>
        /// <param name="uid">Uid to identify request</param>
        /// <returns></returns>
        private async Task Send(string eventName, JObject o, string? uid = null)
        {
            string data = o.ToString(Formatting.None);
            await Send(eventName, data, uid);
        }

        /// <summary>
        /// Send a msg though this connection
        /// </summary>
        /// <param name="dataToSend">Object to send</param>
        /// <returns></returns>
        internal async Task Send(byte[] dataToSend)
        {
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
        /// <summary>
        /// Send a msg though this connection
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="obj"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        public async Task Send(string eventName, object? obj = null, string? uid = null)
        {
            try
            {
                if (obj != null)
                {
                    string json = JsonConvert.SerializeObject(obj, instance.converter);
                    await Send(eventName, json, uid);
                }
                else
                {
                    await Send(eventName, new JObject(), uid);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion
    }


}
