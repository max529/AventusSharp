using System;

namespace AventusSharp.WebSocket.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EndPoint : Attribute
    {
        public Type endpoint { get; private set; }

        public string typescriptPath { get; protected set; }

        public EndPoint(Type type)
        {
            endpoint = type;
        }

        public EndPoint(Type type, string typescriptPath)
        {
            endpoint = type;
            this.typescriptPath = typescriptPath;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class EndPoint<T> : EndPoint where T : IWsEndPoint, new()
    {
        
        public EndPoint() : base(typeof(T))
        {
        }

        public EndPoint(string typescriptPath) : base(typeof(T), typescriptPath)
        {
        }
    }
}
