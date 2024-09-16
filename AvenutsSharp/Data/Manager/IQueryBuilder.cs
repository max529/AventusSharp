using AventusSharp.Data.Manager.DB;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{

    public interface IQueryBuilder<T>
    {
        public List<T> Run();
        public ResultWithError<List<T>> RunWithError();
        public T? Single();
        public ResultWithError<T> SingleWithError();

        public IQueryBuilder<T> Prepare(params object[] objects);
        public IQueryBuilder<T> SetVariable(string name, object value);

        public IQueryBuilder<T> Where(Expression<Func<T, bool>> func);
        public IQueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);

        public IQueryBuilder<T> Field<U>(Expression<Func<T, U>> memberExpression);
        public IQueryBuilder<T> Sort<U>(Expression<Func<T, U>> expression, Sort? sort);
        public IQueryBuilder<T> Include(Expression<Func<T, IStorable>> memberExpression);
        public IQueryBuilder<T> Limit(int? limit);
        public IQueryBuilder<T> Offset(int? offset);
        public IQueryBuilder<T> Take(int length, int? offset);
    }


}
