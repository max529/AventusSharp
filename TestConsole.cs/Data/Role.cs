using AventusSharp.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole.cs.Data
{
    public class Role : Storable<Person>
    {
        public string name { get; set; }
    }
}
