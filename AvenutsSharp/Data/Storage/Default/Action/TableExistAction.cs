using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class TableExistAction<T> : GenericAction<T> where T : IStorage
    {
        public abstract bool run(TableInfo table);
        
    }
}
