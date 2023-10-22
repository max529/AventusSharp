using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager
{
    public interface IDeleteBuilder<T>
    {
        public List<T>? Run();
        public ResultWithDataError<List<T>> RunWithError();

        public IDeleteBuilder<T> Prepare(params object[] objects);
        public IDeleteBuilder<T> SetVariable(string name, object value);

        public IDeleteBuilder<T> Where(Expression<Func<T, bool>> func);
        public IDeleteBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);
    }
}
