using System;

namespace AventusSharp.Routes.Attributes
{
    
    [AttributeUsage(AttributeTargets.Method)]
    public class Path : Attribute
    {
        public string pattern { get; private set; }
        public Path(string pattern)
        {
            this.pattern = pattern;
        }
    }
}
