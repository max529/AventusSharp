﻿using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket.Event
{
    internal class WsEventConfig
    {
        public static Dictionary<Type, WsEventConfig> configs = new Dictionary<Type, WsEventConfig>();

        public WsEndPoint endPoint;
        public string path;
        public ResponseTypeEnum eventType;

        public WsEventConfig(WsEndPoint endPoint, string path, ResponseTypeEnum eventType)
        {
            this.endPoint = endPoint;
            this.path = path;
            this.eventType = eventType;
        }
    }

    [NoTypescript]
    public abstract class WsEvent<T> : WebSocketEvent
    {
        public WsEvent()
        {
            WsEventConfig config = GetConfig();
            this.path = config.path;
            this.endPoint = config.endPoint;
            this.eventType = config.eventType;

        }

        private WsEventConfig GetConfig()
        {
            Type type = this.GetType();
            if (!WsEventConfig.configs.ContainsKey(type))
            {
                WsEndPoint endPoint;
                string path;
                WebSocketAttributeAnalyze infoMethod = WebSocketMiddleware.PrepareAttributes(GetType().GetCustomAttributes());
                if (infoMethod.endPoints.Count == 0)
                {
                    endPoint = WebSocketMiddleware.GetMain();
                }
                else
                {
                    endPoint = infoMethod.endPoints[0];
                }

                if (infoMethod.pathes.Count == 0)
                {
                    path = this.GetType().FullName ?? "";//prepare base on full classname;
                }
                else
                {
                    path = infoMethod.pathes[0];
                }
                WsEventConfig.configs[type] = new WsEventConfig(endPoint, path, infoMethod.eventType);
            }

            return WsEventConfig.configs[type];
        }


        public override async Task Emit()
        {
            T o = await Prepare();
            await DefaultEmit(o);
        }

        protected abstract Task<T> Prepare();
    }
}
