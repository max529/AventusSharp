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
        List<X> Create<X>(List<X> values);
        X Create<X>(X value);

    }
    public interface IGenericDM<U> : IGenericDM where U : IStorable
    {
        List<U> GetAll();
        new List<X> GetAll<X>() where X : U;

        new List<X> Create<X>(List<X> values) where X : U;
        new X Create<X>(X value) where X : U;
    }
}
