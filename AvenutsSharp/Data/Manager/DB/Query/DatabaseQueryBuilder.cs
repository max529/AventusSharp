﻿using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Query
{
    public class DatabaseQueryBuilder<T> : DatabaseGenericBuilder<T>, IQueryBuilder<T>, ILambdaTranslatable where T : IStorable
    {
        public bool AllMembers { get; set; } = true;

        public bool UseShortObject { get; set; } = true;

        public DatabaseQueryBuilder(IDBStorage storage) : base(storage)
        {

        }

        public List<T> Run()
        {
            ResultWithDataError<List<T>> result = Storage.QueryFromBuilder(this);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<T>();
        }
        public ResultWithDataError<List<T>> RunWithError()
        {
            return Storage.QueryFromBuilder(this);
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

        public IQueryBuilder<T> Field(Expression<Func<T, object>> expression)
        {
            FieldGeneric(expression);
            AllMembers = false;
            return this;
        }

        public IQueryBuilder<T> Include(Expression<Func<T, IStorable>> expression)
        {
            IncludeGeneric(expression);
            return this;
        }
    }
}
