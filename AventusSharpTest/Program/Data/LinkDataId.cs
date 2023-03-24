using AventusSharp.Attributes;
using AventusSharp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Data
{
    public class LinkDataId : Storable<SimpleData>
    {
        public string name { get; set; }

        [ForeignKey(typeof(SimpleData))]
        public int simpleDataId { get; set; }
    }
}
