using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Attributes
{
    /// <summary>
    /// Attribute used for pushin the class fileds inside children
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ForceInherit : System.Attribute
    {
    }
}
