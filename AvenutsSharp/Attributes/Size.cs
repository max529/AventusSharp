using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class Size : System.Attribute
    {
        public int nb { get; private set; }
        public bool max { get; private set; }
        /**
         * if nb = -1
         */
        public Size(int nb)
        {
            this.nb = nb;
            if(nb <= 0)
            {
                max = true;
            }
        }
        public Size(bool max)
        {
            this.max = max;
            if (!max)
            {
                nb = 255;
            }
        }
    }
}
