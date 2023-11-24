using System;

namespace AventusSharp.WebSocket.Attributes
{

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class ListenOnBoot : Attribute
    {
        public bool listen { get; private set; }
        public ListenOnBoot(bool listen = true)
        {
            this.listen = listen;
        }
    }
}
