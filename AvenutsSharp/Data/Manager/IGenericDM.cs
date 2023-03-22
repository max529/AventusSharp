using System;
using System.Collections.Generic;
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

        List<X> GetAll<X>();

        #region Create
        List<X> Create<X>(List<X> values);
        ResultWithError<List<X>> CreateWithError<X>(List<X> values);
        X Create<X>(X value);
        ResultWithError<X> CreateWithError<X>(X value);
        #endregion

    }
    public interface IGenericDM<U> : IGenericDM where U : IStorable
    {
        List<U> GetAll();
        new List<X> GetAll<X>() where X : U;

        #region Create
        new List<X> Create<X>(List<X> values) where X : U;
        new ResultWithError<List<X>> CreateWithError<X>(List<X> values) where X : U;
        new X Create<X>(X value) where X : U;
        new ResultWithError<X> CreateWithError<X>(X value) where X : U;
        #endregion
    }
}
