﻿using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;

namespace AventusSharp.Data.Manager.DB.Create
{
    public class DatabaseCreateBuilderInfo
    {
        public string Sql { get; set; }
        public bool HasPrimaryResult { get; set; }
        public List<ParamsInfo> Parameters { get; }

        public ParamsInfo? PrimaryToSet {get; set;}

        public DatabaseCreateBuilderInfo(string sql, bool havePrimaryResult, List<ParamsInfo> parameters)
        {
            Sql = sql;
            HasPrimaryResult = havePrimaryResult;
            Parameters = parameters;
        }

    }
    public class DatabaseCreateBuilder<T> where T : IStorable
    {
        public IDBStorage Storage { get; private set; }
        public TableInfo TableInfo { get; private set; }

        public List<DatabaseCreateBuilderInfo>? queries;

        public ParamsInfo? PrimaryParam { get; set; }

        public DatabaseCreateBuilder(IDBStorage storage, Type? baseType = null)
        {
            Storage = storage;
            if (baseType == null)
            {
                baseType = typeof(T);
            }
            TableInfo tableInfo = Storage.GetTableInfo(baseType) ?? throw new Exception();
            TableInfo = tableInfo;
        }

        public ResultWithError<T> RunWithError(T item)
        {
            ResultWithError<T> result = new();
            ResultWithError<int> resultTemp = Storage.CreateFromBuilder(this, item);
            if (resultTemp.Success && resultTemp.Result != 0)
            {
                item.id = resultTemp.Result;
                result.Result = item;
            }
            else
            {
                result.Errors.AddRange(resultTemp.Errors);
            }

            return result;
        }

    }
}