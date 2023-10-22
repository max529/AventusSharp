using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    public interface IGenericDM
    {
        Type GetMainType();
        List<Type> DefineManualDependances();
        string Name { get; }
        bool IsInit { get; }

        Task<VoidWithDataError> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config);
        Task<VoidWithDataError> Init();

        #region Get
        List<X> GetAll<X>() where X : notnull;
        ResultWithDataError<List<X>> GetAllWithError<X>() where X : notnull;
        IQueryBuilder<X> CreateQuery<X>() where X : notnull;
        IUpdateBuilder<X>? CreateUpdate<X>() where X : notnull;
        IDeleteBuilder<X>? CreateDelete<X>() where X : notnull;
        IExistBuilder<X>? CreateExist<X>() where X : notnull;

        object? GetById(int id);
        X GetById<X>(int id) where X : notnull;
        ResultWithDataError<X> GetByIdWithError<X>(int id) where X : notnull;

        List<X> GetByIds<X>(List<int> ids) where X : notnull;
        ResultWithDataError<List<X>> GetByIdsWithError<X>(List<int> ids) where X : notnull;

        List<X> Where<X>(Expression<Func<X, bool>> func) where X : notnull;
        ResultWithDataError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : notnull;

        bool Exist<X>(Expression<Func<X, bool>> func) where X : notnull;
        ResultWithDataError<bool> ExistWithError<X>(Expression<Func<X, bool>> func) where X : notnull;
        #endregion

        #region Create
        List<X> Create<X>(List<X> values) where X : notnull, IStorable;
        ResultWithDataError<List<X>> CreateWithError<X>(List<X> values) where X : notnull, IStorable;
        X? Create<X>(X value) where X : notnull, IStorable;
        ResultWithDataError<X> CreateWithError<X>(X value) where X : notnull, IStorable;
        #endregion

        #region Update
        List<X> Update<X>(List<X> values) where X : notnull, IStorable;
        ResultWithDataError<List<X>> UpdateWithError<X>(List<X> values) where X : notnull, IStorable;
        X Update<X>(X value) where X : notnull, IStorable;
        ResultWithDataError<X> UpdateWithError<X>(X value) where X : notnull, IStorable;
        #endregion

        #region Delete
        List<X> Delete<X>(List<X> values) where X : notnull, IStorable;
        ResultWithDataError<List<X>> DeleteWithError<X>(List<X> values) where X : notnull, IStorable;
        X Delete<X>(X value) where X : notnull, IStorable;
        ResultWithDataError<X> DeleteWithError<X>(X value) where X : notnull, IStorable;
        #endregion

    }
    public interface IGenericDM<U> : IGenericDM where U : notnull, IStorable
    {
        #region Get
        new List<X> GetAll<X>() where X : U;
        new ResultWithDataError<List<X>> GetAllWithError<X>() where X : U;
        new IQueryBuilder<X>? CreateQuery<X>() where X : U;
        new IUpdateBuilder<X>? CreateUpdate<X>() where X : U;

        new X? GetById<X>(int id) where X : U;
        new ResultWithDataError<X> GetByIdWithError<X>(int id) where X : U;

        new List<X>? GetByIds<X>(List<int> ids) where X : U;
        new ResultWithDataError<List<X>> GetByIdsWithError<X>(List<int> id) where X : U;

        new List<X> Where<X>(Expression<Func<X, bool>> func) where X : U;
        new ResultWithDataError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : U;

        #endregion

        #region Create
        new List<X> Create<X>(List<X> values) where X : U;
        new ResultWithDataError<List<X>> CreateWithError<X>(List<X> values) where X : U;
        new X? Create<X>(X value) where X : U;
        new ResultWithDataError<X> CreateWithError<X>(X value) where X : U;
        #endregion

        #region Update
        new List<X> Update<X>(List<X> values) where X : U;
        new ResultWithDataError<List<X>> UpdateWithError<X>(List<X> values) where X : U;
        new X? Update<X>(X value) where X : U;
        new ResultWithDataError<X> UpdateWithError<X>(X value) where X : U;
        #endregion

        #region Delete
        new List<X> Delete<X>(List<X> values) where X : U;
        new ResultWithDataError<List<X>> DeleteWithError<X>(List<X> values) where X : U;
        new X? Delete<X>(X value) where X : U;
        new ResultWithDataError<X> DeleteWithError<X>(X value) where X : U;
        #endregion
    }
}
