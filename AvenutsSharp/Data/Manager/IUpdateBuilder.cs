﻿using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager
{
    public interface IUpdateBuilder<T>
    {
        public List<T>? Run(T item);
        public ResultWithError<List<T>> RunWithError(T item);
        public ResultWithError<T> RunWithErrorSingle(T item);

        public IUpdateBuilder<T> Field<U>(Expression<Func<T, U>> fct);

        public IUpdateBuilder<T> Prepare(params object[] objects);
        public IUpdateBuilder<T> SetVariable(string name, object value);

        public IUpdateBuilder<T> Where(Expression<Func<T, bool>> func);
        public IUpdateBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func);
    }
}
