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
        Type getMainType();
        List<Type> defineManualDependances();
        string Name { get; }
        bool isInit { get; }

        Task<bool> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config);
        Task<bool> Init();

        #region Get
        List<X> GetAll<X>() where X : notnull;
        ResultWithError<List<X>> GetAllWithError<X>() where X : notnull;
        QueryBuilder<X>? CreateQuery<X>() where X : notnull;

        X GetById<X>(int id) where X : notnull;
        ResultWithError<X> GetByIdWithError<X>(int id) where X : notnull;

        List<X> Where<X>(Expression<Func<X, bool>> func) where X : notnull;
        ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : notnull;
        #endregion

        #region Create
        List<X> Create<X>(List<X> values) where X : notnull;
        ResultWithError<List<X>> CreateWithError<X>(List<X> values) where X : notnull;
        X Create<X>(X value) where X : notnull;
        ResultWithError<X> CreateWithError<X>(X value) where X : notnull;
        #endregion

        #region Update
        List<X> Update<X>(List<X> values) where X : notnull;
        ResultWithError<List<X>> UpdateWithError<X>(List<X> values) where X : notnull;
        X Update<X>(X value) where X : notnull;
        ResultWithError<X> UpdateWithError<X>(X value) where X : notnull;
        #endregion

        #region Delete
        List<X> Delete<X>(List<X> values) where X : notnull;
        ResultWithError<List<X>> DeleteWithError<X>(List<X> values) where X : notnull;
        X Delete<X>(X value) where X : notnull;
        ResultWithError<X> DeleteWithError<X>(X value) where X : notnull;
        #endregion

    }
    public interface IGenericDM<U> : IGenericDM where U : notnull, IStorable
    {
        #region Get
        new List<X> GetAll<X>() where X : U;
        new ResultWithError<List<X>> GetAllWithError<X>() where X : U;
        new QueryBuilder<X>? CreateQuery<X>() where X : U;

        new X? GetById<X>(int id) where X : U;
        new ResultWithError<X> GetByIdWithError<X>(int id) where X : U;

        new List<X> Where<X>(Expression<Func<X, bool>> func) where X : U;
        new ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : U;

        #endregion

        #region Create
        new List<X> Create<X>(List<X> values) where X : U;
        new ResultWithError<List<X>> CreateWithError<X>(List<X> values) where X : U;
        new X? Create<X>(X value) where X : U;
        new ResultWithError<X> CreateWithError<X>(X value) where X : U;
        #endregion

        #region Update
        new List<X> Update<X>(List<X> values) where X : U;
        new ResultWithError<List<X>> UpdateWithError<X>(List<X> values) where X : U;
        new X? Update<X>(X value) where X : U;
        new ResultWithError<X> UpdateWithError<X>(X value) where X : U;
        #endregion

        #region Delete
        new List<X> Delete<X>(List<X> values) where X : U;
        new ResultWithError<List<X>> DeleteWithError<X>(List<X> values) where X : U;
        new X? Delete<X>(X value) where X : U;
        new ResultWithError<X> DeleteWithError<X>(X value) where X : U;
        #endregion
    }
}
