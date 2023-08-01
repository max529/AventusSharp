using System;

namespace AventusSharp.Tools.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface)]
    public class Typescript : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
    public class NoTypescript : Attribute
    {

    }
}
