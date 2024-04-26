using System;

namespace AventusSharp.WebSocket.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class Prefix : Attribute
    {
        public string txt { get; private set; }
        public Prefix(string txt)
        {
            this.txt = txt;
        }
    }
}
