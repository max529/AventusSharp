using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AventusSharp.WebSocket
{
    public enum WebSocketEventType
    {
        Response,
        Others,
        Broadcast
    }
    public class WebSocketRouteInfo
    {
        public Regex pattern;
        public MethodInfo action;
        public IWsRoute router;
        public int nbParamsFunction;
        public Dictionary<string, WebSocketRouterParameterInfo> parameters = new Dictionary<string, WebSocketRouterParameterInfo>();
        public WebSocketEventType eventType;
        public string UniqueKey
        {
            get => pattern.ToString();
        }

        public WebSocketRouteInfo(Regex pattern, MethodInfo action, IWsRoute router, int nbParamsFunction, WebSocketEventType eventType)
        {
            this.pattern = pattern;
            this.action = action;
            this.router = router;
            this.nbParamsFunction = nbParamsFunction;
            this.eventType = eventType;
        }

        public override string ToString()
        {
            return pattern.ToString();
        }
    }

    public class WebSocketRouterParameterInfo
    {
        public string name;
        public Type type;
        public int positionCSharp = -1;
        public int positionUrl = -1;

        public WebSocketRouterParameterInfo(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }
    }
}
