using System;

namespace AventusSharp.Data.Attributes
{
    /**
     * Use it to add a link to your database (works only on int)
     */
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForeignKey : System.Attribute
    {
        public Type Type { get; private set; }
        public ForeignKey(Type type)
        {
            Type = type;
        }
    }
}
