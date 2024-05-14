﻿using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.Dummy
{
    public class DummyQueryBuilder<T> : IQueryBuilder<T>
    {
        public IQueryBuilder<T> Field<U>(Expression<Func<T, U>> memberExpression)
        {
            throw new NotImplementedException();
        }
        
        public IQueryBuilder<T> Include(Expression<Func<T, IStorable>> memberExpression)
        {
            throw new NotImplementedException();
        }


        public IQueryBuilder<T> Prepare(params object[] objects)
        {
            throw new NotImplementedException();
        }

        public List<T> Run()
        {
            throw new NotImplementedException();
        }

        public ResultWithError<List<T>> RunWithError()
        {
            throw new NotImplementedException();
        }

        public IQueryBuilder<T> SetVariable(string name, object value)
        {
            throw new NotImplementedException();
        }

        public T? Single()
        {
            throw new NotImplementedException();
        }

        public ResultWithError<T> SingleWithError()
        {
            throw new NotImplementedException();
        }

        public IQueryBuilder<T> Where(Expression<Func<T, bool>> func)
        {
            throw new NotImplementedException();
        }


        public IQueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func)
        {
            throw new NotImplementedException();
        }
        
    }
}
