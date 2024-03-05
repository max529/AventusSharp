using System;

namespace AventusSharp.Tools.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface)]
    public class Typescript : Attribute
    {
        public string? Namespace;
        public bool? Internal;

        public Typescript() { }

        public Typescript(string _namespace)
        {
            Namespace = _namespace;
        }

        public Typescript(bool _internal)
        {
            Internal = _internal;
        }

        public Typescript(string _namespace, bool _internal)
        {
            Namespace = _namespace;
            Internal = _internal;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class NoTypescript : Attribute
    {

    }
}
