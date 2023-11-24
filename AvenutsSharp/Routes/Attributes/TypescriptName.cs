using System;

namespace AventusSharp.Routes.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TypescriptName : Attribute
    {
        public string name { get; private set; }
        public TypescriptName(string name)
        {
            this.name = name;
        }
    }
}
