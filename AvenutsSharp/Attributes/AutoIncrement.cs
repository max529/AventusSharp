using System;
using System.Collections.Generic;
using System.Text;

namespace AvenutsSharp.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class AutoIncrement : System.Attribute
    {
    }
}
