using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    public interface QueryBuilder<T>
    {
        public List<T> Run();
        public ResultWithError<List<T>> RunWithError();

        public QueryBuilder<T> Prepare(params object[] objects);
        public QueryBuilder<T> SetVariable(string name, object value);

        public QueryBuilder<T> Where(Expression<Func<T, bool>> func);
        public QueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);

        public QueryBuilder<T> Field(Expression<Func<T, object>> memberExpression);

        public QueryBuilder<T> Include(Expression<Func<T, IStorable>> memberExpression);
    }


}
