using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager
{
    public interface IExistBuilder<T>
    {
        public bool Run();
        public ResultWithDataError<bool> RunWithError();

        public IExistBuilder<T> Prepare(params object[] objects);
        public IExistBuilder<T> SetVariable(string name, object value);

        public IExistBuilder<T> Where(Expression<Func<T, bool>> func);
        public IExistBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);

    }


}
