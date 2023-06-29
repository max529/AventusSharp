using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;

namespace AventusSharp.Data.Storage.Default
{
    public interface IStorage
    {
        public bool IsConnectedOneTime { get; }
        public void CreateLinks();
        public void AddPyramid(PyramidInfo pyramid);
        public TableInfo? GetTableInfo(Type type);
        public ResultWithError<List<X>> QueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder);
        public ResultWithError<X> UpdateFromBuilder<X>(DatabaseUpdateBuilder<X> queryBuilder, X item);
        public VoidWithError CreateTable(PyramidInfo pyramid);
        public ResultWithError<bool> TableExist(PyramidInfo pyramid);
        public ResultWithError<List<X>> Create<X>(List<X> values) where X : IStorable;
        public ResultWithError<List<X>> Update<X>(List<X> values, List<X>? oldValues) where X : IStorable;
        public ResultWithError<List<X>> Delete<X>(List<X> values) where X : IStorable;

        public ResultWithError<DbTransaction> BeginTransaction();
        public ResultWithError<bool> CommitTransaction(DbTransaction transaction);
        public ResultWithError<bool> RollbackTransaction(DbTransaction transaction);

        public bool Connect();
        public ResultWithError<bool> ResetStorage();

        public string GetDatabaseName();
    }


}
