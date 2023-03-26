using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class WhereAction<T> : GenericAction<T> where T : IStorage
    {
        public abstract ResultWithError<List<X>> run<X>(TableInfo table, Expression<Func<X, bool>> func) where X : IStorable;
    }
}
