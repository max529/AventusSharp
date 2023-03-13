using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Attributes
{
    /**
     * Use it to add a link to your database (works only on int)
     */
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForeignKey : System.Attribute
    {
        public Type type { get; private set; }
        public ForeignKey(Type type)
        {
            this.type = type;
        }
    }
}
