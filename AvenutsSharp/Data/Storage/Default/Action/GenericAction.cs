using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    public abstract class GenericAction<T> where T : IStorage
    {
        public T Storage { get; set; }
        
    }
}
