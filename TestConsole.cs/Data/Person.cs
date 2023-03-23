﻿using AventusSharp.Attributes;
using AventusSharp.Data;
using AvenutsSharp.Attributes;
using System.Collections.Generic;
using System.Text;

namespace TestConsole.cs.Data
{
    
    public class Person : Storable<Person>
    {
        public string firstname { get; set; }

        public string lastname { get; set; }

        [Nullable]
        public Location location { get; set; }
        
        [NotInDB]
        public Role role { get; set; }
    }
}