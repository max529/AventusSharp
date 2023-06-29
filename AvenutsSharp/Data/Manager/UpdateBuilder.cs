using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager
{
    public interface UpdateBuilder<T>
    {
        public T? Run(T item);
        public ResultWithError<T> RunWithError(T item);

        public UpdateBuilder<T> UpdateField(Expression<Func<T, object>> fct);

        public UpdateBuilder<T> Prepare(params object[] objects);
        public UpdateBuilder<T> SetVariable(string name, object value);

        public UpdateBuilder<T> Where(Expression<Func<T, bool>> func);
        public UpdateBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);
    }
}
