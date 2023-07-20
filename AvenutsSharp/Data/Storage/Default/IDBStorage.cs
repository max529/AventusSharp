using AventusSharp.Data.Manager.DB.Create;
using AventusSharp.Data.Manager.DB.Delete;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace AventusSharp.Data.Storage.Default
{
    public interface IDBStorage
    {
        public bool IsConnectedOneTime { get; }
        public VoidWithError CreateLinks();
        public VoidWithError AddPyramid(PyramidInfo pyramid);
        public TableInfo? GetTableInfo(Type type);
        public ResultWithError<List<X>> QueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder) where X : IStorable;
        public ResultWithError<int> CreateFromBuilder<X>(DatabaseCreateBuilder<X> queryBuilder, X item) where X : IStorable;
        public ResultWithError<List<int>> UpdateFromBuilder<X>(DatabaseUpdateBuilder<X> queryBuilder, X item) where X : IStorable;
        public VoidWithError DeleteFromBuilder<X>(DatabaseDeleteBuilder<X> queryBuilder) where X : IStorable;
        public VoidWithError CreateTable(PyramidInfo pyramid);
        public ResultWithError<bool> TableExist(PyramidInfo pyramid);

        public ResultWithError<DbTransaction> BeginTransaction();
        public ResultWithError<bool> CommitTransaction(DbTransaction transaction);
        public ResultWithError<bool> RollbackTransaction(DbTransaction transaction);

        public VoidWithError ConnectWithError();
        public ResultWithError<bool> ResetStorage();

        public string GetDatabaseName();
        public ResultWithError<Dictionary<TableInfo, IList>> GroupDataByType<X>(IList data);

        public ResultWithError<Y> RunInsideTransaction<Y>(Y defaultValue, Func<ResultWithError<Y>> action);
    }


}
