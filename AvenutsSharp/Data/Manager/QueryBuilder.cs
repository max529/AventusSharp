using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    public abstract class QueryBuilder<T>
    {
        public QueryBuilder()
        {
        }

        public abstract void Execute();

        public abstract List<T> Query();

        public abstract QueryBuilder<T> Prepare(params object[] objects);
        public abstract QueryBuilder<T> SetVariable(string name, object value);

        public abstract QueryBuilder<T> Where(Expression<Func<T, bool>> func);
        public abstract QueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);

        public abstract QueryBuilder<T> Field(Expression<Func<T, object>> memberExpression);

        public abstract QueryBuilder<T> Include(Expression<Func<T, IStorable>> memberExpression);
    }


}
