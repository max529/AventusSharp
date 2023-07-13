using System;

namespace AvenutsSharp.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotInDB : Attribute
    {
    }
}
