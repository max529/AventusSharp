﻿using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Builders
{
    public class DatabaseQueryBuilderInfo
    {
        public string Sql;

        public DatabaseQueryBuilderInfo(string sql)
        {
            Sql = sql;
        }
    }


    public class DatabaseQueryBuilder<T> : DatabaseGenericBuilder<T>, IQueryBuilder<T>, ILambdaTranslatable where T : IStorable
    {

        public DatabaseQueryBuilderInfo? info = null;
        public bool UseShortObject { get; set; } = true;

        public DatabaseQueryBuilder(IDBStorage storage, IGenericDM DM) : base(storage, DM)
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
            var result = Storage.QueryFromBuilder(this);
            DM.PrintErrors(result);
            return result;

        }

        public T? Single()
        {
            return SingleWithError().Result;
        }
        public ResultWithError<T> SingleWithError()
        {
            ResultWithError<T> result = new ResultWithError<T>();
            ResultWithError<List<T>> runResult = RunWithError();
            result.Errors = runResult.Errors;
            if (runResult.Result != null && runResult.Result.Count > 0)
            {
                result.Result = runResult.Result[0];
            }
            return result;

        }

        public IQueryBuilder<T> Where(Expression<Func<T, bool>> expression)
        {
            WhereGeneric(expression);
            return this;
        }

        public IQueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> expression)
        {
            WhereGenericWithParameters(expression);
            return this;
        }

        public IQueryBuilder<T> Prepare(params object[] objects)
        {
            PrepareGeneric(objects);
            return this;
        }
        public IQueryBuilder<T> SetVariable(string name, object value)
        {
            SetVariableGeneric(name, value);
            return this;
        }

        public IQueryBuilder<T> Field<U>(Expression<Func<T, U>> expression)
        {
            FieldGeneric(expression);
            return this;
        }

        public IQueryBuilder<T> Sort<U>(Expression<Func<T, U>> expression, Sort sort = DB.Sort.ASC)
        {
            SortGeneric(expression, sort);
            return this;
        }

        public IQueryBuilder<T> Include(Expression<Func<T, IStorable>> expression)
        {
            IncludeGeneric(expression);
            return this;
        }

        public IQueryBuilder<T> Limit(int limit)
        {
            LimitGeneric(limit);
            return this;
        }

        public IQueryBuilder<T> Offset(int offset)
        {
            OffsetGeneric(offset);
            return this;
        }
    }
}
