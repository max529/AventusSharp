using AventusSharp;
using AventusSharp.Attributes;
using AventusSharp.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole.cs.Data
{
    public class Location : Storable<Location>
    {
        public string name { get; set; }

        [AutoCreate, AutoUpdate]
        public ICountry country { get; set; }
    }
}
