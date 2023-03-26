using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class CreateAction<T> : GenericAction<T> where T : IStorage
    {
        /// <summary>
        /// Run the action to Create data inside the store
        /// This function is already wrapped by a transaction
        /// </summary>
        /// <typeparam name="X"></typeparam>
        /// <param name="table"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public abstract ResultWithError<List<X>> run<X>(TableInfo table, List<X> data) where X : IStorable;
    }
}
