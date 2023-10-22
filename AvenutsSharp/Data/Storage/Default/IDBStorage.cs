using AventusSharp.Data.Manager.DB.Create;
using AventusSharp.Data.Manager.DB.Delete;
using AventusSharp.Data.Manager.DB.Exist;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace AventusSharp.Data.Storage.Default
{
    public interface IDBStorage
    {
        public bool IsConnectedOneTime { get; }
        public bool Debug { get; set; }
        public VoidWithDataError CreateLinks();
        public VoidWithDataError AddPyramid(PyramidInfo pyramid);
        public TableInfo? GetTableInfo(Type type);
        public ResultWithDataError<List<X>> QueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder) where X : IStorable;
        public ResultWithDataError<bool> ExistFromBuilder<X>(DatabaseExistBuilder<X> queryBuilder) where X : IStorable;
        public ResultWithDataError<int> CreateFromBuilder<X>(DatabaseCreateBuilder<X> queryBuilder, X item) where X : IStorable;
        public ResultWithDataError<List<int>> UpdateFromBuilder<X>(DatabaseUpdateBuilder<X> queryBuilder, X item) where X : IStorable;
        public VoidWithDataError DeleteFromBuilder<X>(DatabaseDeleteBuilder<X> queryBuilder) where X : IStorable;
        public VoidWithDataError CreateTable(PyramidInfo pyramid);
        public ResultWithDataError<bool> TableExist(PyramidInfo pyramid);

        public ResultWithDataError<BeginTransactionResult> BeginTransaction();
        public ResultWithDataError<bool> CommitTransaction(DbTransaction transaction);
        public ResultWithDataError<bool> RollbackTransaction(DbTransaction transaction);

        public VoidWithDataError ConnectWithError();
        public ResultWithDataError<bool> ResetStorage();

        public string GetDatabaseName();
        public ResultWithDataError<Dictionary<TableInfo, IList>> GroupDataByType<X>(IList data);

        public ResultWithDataError<Y> RunInsideTransaction<Y>(Y defaultValue, Func<ResultWithDataError<Y>> action);
    }


}
