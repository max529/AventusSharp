using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class CreateAction<T> : GenericAction<T> where T : IStorage
    {
        public abstract List<X> run<X>(TableInfo table, List<X> data) where X : IStorable;
    }
}
