using System;

namespace AventusSharp.Route.Attributes
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
