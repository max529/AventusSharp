using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB.Query
{
    public class DatabaseQueryBuilder<T> : DatabaseGenericBuilder<T>, QueryBuilder<T>, ILambdaTranslatable
    {
        public bool allMembers { get; set; } = true;

        public DatabaseQueryBuilder(IStorage storage) : base(storage)
        {
           
        }

        public List<T> Run()
        {
            ResultWithError<List<T>> result = Storage.QueryFromBuilder(this);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<T>();
        }
        public ResultWithError<List<T>> RunWithError()
        {
            return Storage.QueryFromBuilder(this);
        }

        public QueryBuilder<T> Where(Expression<Func<T, bool>> expression)
        {
            _Where(expression);
            return this;
        }

        public QueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> expression)
        {
            _WhereWithParameters(expression);
            return this;
        }

        public QueryBuilder<T> Prepare(params object[] objects)
        {
           _Prepare(objects);
            return this;
        }
        public QueryBuilder<T> SetVariable(string name, object value)
        {
           _SetVariable(name, value);
            return this;
        }

        public QueryBuilder<T> Field(Expression<Func<T, object>> expression)
        {
            _Field(expression);
            allMembers = false;
            return this;
        }

        public QueryBuilder<T> Include(Expression<Func<T, IStorable>> expression)
        {
            _Include(expression);
            return this;
        }

    }
}
