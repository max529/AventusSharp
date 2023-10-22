using System;

namespace AventusSharp.Tools.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface)]
    public class Typescript : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.All)]
    public class NoTypescript : Attribute
    {

    }
}
