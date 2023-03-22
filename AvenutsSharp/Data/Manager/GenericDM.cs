using AventusSharp.Data.Storage.Default;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    public static class GenericDM
    {
        private static Dictionary<Type, IGenericDM> dico = new Dictionary<Type, IGenericDM>();


        public static IGenericDM Get<U>() where U : IStorable
        {
            if (dico.ContainsKey(typeof(U)))
            {
                return dico[typeof(U)];
            }
            throw new DataError(DataErrorCode.DMNotExist, "Can't found a data manger for type " + typeof(U).Name).GetException();
        }
        public static void Set(Type type, IGenericDM manager)
        {
            if (dico.ContainsKey(type))
            {
                throw new DataError(DataErrorCode.DMAlreadyExist, "A manager already exists for type " + type.Name).GetException();
            }
            dico[type] = manager;
        }
    }
    public abstract class GenericDataManager<T, U> : IGenericDM<U> where T : IGenericDM<U>, new() where U : IStorable
    {
        #region singleton
        private static readonly Mutex mutexGetInstance = new Mutex();
        private static readonly Dictionary<Type, T> instances = new Dictionary<Type, T>();
        ///// <summary>
        ///// Singleton pattern
        ///// </summary>
        ///// <returns></returns>
        public static T getInstance()
        {
            mutexGetInstance.WaitOne();
            if (!instances.ContainsKey(typeof(T)))
            {
                instances.Add(typeof(T), new T());
            }
            mutexGetInstance.ReleaseMutex();
            return instances[typeof(T)];
        }
        #endregion

        #region definition
        public Type getMainType()
        {
            return typeof(U);
        }
        public virtual List<Type> defineManualDependances()
        {
            return new List<Type>();
        }
        public string Name
        {
            get => GetType().Name.Split('`')[0] + "<" + typeof(U).Name.Split('`')[0] + ">";
        }
        public bool isInit { get; protected set; }
        #endregion

        protected PyramidInfo pyramidInfo { get; set; }
        private Dictionary<Type, PyramidInfo> pyramidsInfo { get; set; } = new Dictionary<Type, PyramidInfo>();
        protected Type rootType { get; set; }
        protected DataManagerConfig config { get; set; }

        protected GenericDataManager()
        {
        }
        public virtual Task<bool> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            pyramidInfo = pyramid;
            pyramidsInfo[pyramid.type] = pyramid;
            if (pyramid.aliasType != null)
            {
                pyramidsInfo[pyramid.aliasType] = pyramid;
            }
            this.config = config;
            SetDMForType(pyramid, true);
            return Task.FromResult(true);
        }

        private void SetDMForType(PyramidInfo pyramid, bool isRoot)
        {
            if (!pyramid.isForceInherit || !isRoot)
            {
                if (rootType == null)
                {
                    rootType = pyramid.type;
                }
                GenericDM.Set(pyramid.type, this);
                pyramidsInfo[pyramid.type] = pyramid;
                if (pyramid.aliasType != null)
                {
                    GenericDM.Set(pyramid.aliasType, this);
                    pyramidsInfo[pyramid.aliasType] = pyramid;
                }
            }
            foreach (PyramidInfo child in pyramid.children)
            {
                SetDMForType(child, false);
            }
        }

        public async Task<bool> Init()
        {
            try
            {
                if (await Initialize())
                {
                    isInit = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }
        protected abstract Task<bool> Initialize();


        #region Get

        public abstract List<X> GetAll<X>() where X : U;
        public List<U> GetAll()
        {
            return this.GetAll<U>();
        }
        List<X> IGenericDM.GetAll<X>()
        {
            return InvokeMethod<List<X>, X>();
        }
        #endregion

        #region Create

        #region List
        public abstract ResultWithError<List<X>> CreateWithError<X>(List<X> values) where X : U;
        ResultWithError<List<X>> IGenericDM.CreateWithError<X>(List<X> values)
        {
            return InvokeMethod<ResultWithError<List<X>>, X>(new object[] { values });
        }
        public List<X> Create<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = CreateWithError(values);
            if (result.Success)
            {
                return result.Result;
            }
            return new List<X>();
        }
        List<X> IGenericDM.Create<X>(List<X> values)
        {
            return InvokeMethod<List<X>, X>(new object[] { values });
        }
        #endregion

        #region Item
        public ResultWithError<X> CreateWithError<X>(X value) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();
            ResultWithError<List<X>> resultList = CreateWithError(new List<X>() { value });
            result.Errors = resultList.Errors;
            if (resultList.Result.Count > 0)
            {
                result.Result = resultList.Result[0];
            }
            else
            {
                result.Result = default;
            }
            return result;
        }
        ResultWithError<X> IGenericDM.CreateWithError<X>(X value)
        {
            return InvokeMethod<ResultWithError<X>, X>(new object[] { value });
        }

        public X Create<X>(X value) where X : U
        {
            ResultWithError<X> result = CreateWithError(value);
            if (result.Success)
            {
                return result.Result;
            }
            return default;
        }
        X IGenericDM.Create<X>(X value)
        {
            return InvokeMethod<X, X>(new object[] { value });
        }
        #endregion

        #endregion


        #region Utils
        protected X InvokeMethod<X, Y>(object[] parameters = null, [CallerMemberName] string name = "")
        {
            if (parameters == null)
            {
                parameters = new object[] { };
            }
            List<Type> types = new List<Type>();
            foreach (object param in parameters)
            {
                types.Add(param.GetType());
            }

            MethodInfo[] methods = this.GetType().GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.Name == name && method.IsGenericMethod)
                {
                    MethodInfo methodType = method.MakeGenericMethod(typeof(Y));
                    if (IsSameParameters(methodType.GetParameters(), types))
                    {
                        return (X)methodType.Invoke(this, parameters);
                    }
                }
            }

            throw new DataError(DataErrorCode.MethodNotFound, "The method " + name + "(" + string.Join(", ", parameters.Select(p => p.GetType().Name)) + ") can't be found").GetException();
        }
        private bool IsSameParameters(ParameterInfo[] parameterInfos, List<Type> types)
        {
            if (parameterInfos.Length == types.Count)
            {
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    if (parameterInfos[i].ParameterType != types[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        #endregion

    }
}
