using System;

namespace AventusSharp.Tools.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class TsObject : Attribute
    {
        public string? Name { get; private set; }

        public TsObject() { }

        public TsObject(string name)
        {
            Name = name;
        }
    }

}
