using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class CreateTableAction<T> : GenericAction<T> where T : IStorage
    {
        public abstract void run(TableInfo table);
    }
}
