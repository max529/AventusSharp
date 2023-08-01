using System;

namespace AventusSharp.Routes.Attributes
{
    
    [AttributeUsage(AttributeTargets.Method)]
    public class Route : Attribute
    {
        public string pattern;
        public Route(string pattern)
        {
            this.pattern = pattern;
        }
    }
}
