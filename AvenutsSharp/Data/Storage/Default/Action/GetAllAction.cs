using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class GetAllAction<T> : GenericAction<T> where T : IStorage
    {
        public abstract ResultWithError<List<X>> run<X>(TableInfo table) where X : IStorable;
    }
}
