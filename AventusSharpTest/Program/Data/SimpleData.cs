using AventusSharp.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Data
{
    public class SimpleData : Storable<SimpleData>
    {
        public string nameProperty { get; set; }

        public string nameField;
    }
}
