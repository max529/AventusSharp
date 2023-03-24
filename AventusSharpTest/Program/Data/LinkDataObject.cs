using AventusSharp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Data
{
    public class LinkDataObject : Storable<SimpleData>
    {
        public string name { get; set; }

        public SimpleData simpleData { get; set; }
    }
}
