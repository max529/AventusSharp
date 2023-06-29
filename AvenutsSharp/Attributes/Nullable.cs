using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Attributes
{
    /// <summary>
    /// Attribute used to allow a null value for the Property
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Nullable : Attribute
    {
    }

    /// <summary>
    /// Attribute used to allow a null value for the Property
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NotNullable : Attribute
    {
    }
}
