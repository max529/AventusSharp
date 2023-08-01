using System;

namespace AventusSharp.Routes.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TypescriptPath : Attribute
    {
        public string path;
        public TypescriptPath(string path)
        {
            this.path = path;
        }
    }
}
