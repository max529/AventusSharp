using AventusSharp;
using AventusSharp.Data.Attributes;
using AventusSharp.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace TestConsole.cs.Data
{
    public class Location : Storable<Location>
    {
        public string Name { get; set; }

        public PersonHuman Human { get; set; }
        //public List<PersonHuman> HumanList { get; set; } = new List<PersonHuman>();
    }
}
