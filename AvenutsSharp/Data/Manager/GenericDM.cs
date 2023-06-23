using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage.Default;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
    public abstract class GenericDM<T, U> : IGenericDM<U> where T : IGenericDM<U>, new() where U : notnull, IStorable
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
        protected Type? rootType { get; set; }
        protected DataManagerConfig? config { get; set; }

#pragma warning disable CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
        protected GenericDM()
#pragma warning restore CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
        {
        }

        #region Config
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
                new DataError(DataErrorCode.UnknowError, e).Print();
            }
            return false;
        }
        protected abstract Task<bool> Initialize();
        #endregion

        #region generic query
        public abstract QueryBuilder<X> CreateQuery<X>() where X : U;
        QueryBuilder<X>? IGenericDM.CreateQuery<X>()
        {
            QueryBuilder<X> result = InvokeMethod<QueryBuilder<X>, X>(new object[] { });
            //if (result == null)
            //{
            //    result = new ResultWithError<QueryBuilder<X>>();
            //    result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method QueryBuilder"));
            //}
            return result;
        }
        #endregion

        #region Get

        #region GetAll
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<List<X>> GetAllWithError<X>() where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.GetAllWithError<X>()
        {
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(new object[] { });
            if (result == null)
            {
                result = new ResultWithError<List<X>>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method GetAllWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public List<X> GetAll<X>() where X : U
        {
            ResultWithError<List<X>> result = GetAllWithError<X>();
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<X>();
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        List<X> IGenericDM.GetAll<X>()
        {
            List<X>? result = InvokeMethod<List<X>, X>(new object[] { });
            if (result == null)
            {
                return new List<X>();
            }
            return result;
        }

        #endregion

        #region GetById
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<X> GetByIdWithError<X>(int id) where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<X> IGenericDM.GetByIdWithError<X>(int id)
        {
            ResultWithError<X>? result = InvokeMethod<ResultWithError<X>, X>(new object[] { id });
            if (result == null)
            {
                result = new ResultWithError<X>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method GetByIdWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public X? GetById<X>(int id) where X : U
        {
            ResultWithError<X> result = GetByIdWithError<X>(id);
            if (result.Success)
            {
                return result.Result;
            }
            return default;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        X IGenericDM.GetById<X>(int id)
        {
            X? result = InvokeMethod<X, X>(new object[] { id });
            if (result == null)
            {
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
                return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
            }
            return result;
        }
        #endregion

        #region Where
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.WhereWithError<X>(Expression<Func<X, bool>> func)
        {
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(new object[] { func });
            if (result == null)
            {
                result = new ResultWithError<List<X>>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method WhereWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public List<X> Where<X>(Expression<Func<X, bool>> func) where X : U
        {
            ResultWithError<List<X>> result = WhereWithError(func);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<X>();
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        List<X> IGenericDM.Where<X>(Expression<Func<X, bool>> func)
        {
            List<X>? result = InvokeMethod<List<X>, X>(new object[] { func }, false);
            if (result == null)
            {
                return new List<X>();
            }
            return result;
        }
        #endregion

        #endregion

        #region Create

        #region List
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<List<X>> CreateWithError<X>(List<X> values) where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.CreateWithError<X>(List<X> values)
        {
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(new object[] { values });
            if (result == null)
            {
                result = new ResultWithError<List<X>>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method CreateWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public List<X> Create<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = CreateWithError(values);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<X>();
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        List<X> IGenericDM.Create<X>(List<X> values)
        {
            List<X>? result = InvokeMethod<List<X>, X>(new object[] { values });
            if (result == null)
            {
                return new List<X>();
            }
            return result;
        }
        #endregion

        #region Item
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> CreateWithError<X>(X value) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();
            ResultWithError<List<X>> resultList = CreateWithError(new List<X>() { value });
            result.Errors = resultList.Errors;
            if (resultList.Result?.Count > 0)
            {
                result.Result = resultList.Result[0];
            }
            else
            {
                result.Result = default;
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<X> IGenericDM.CreateWithError<X>(X value)
        {
            if (value != null)
            {
                ResultWithError<X>? result = InvokeMethod<ResultWithError<X>, X>(new object[] { value });
                if (result == null)
                {
                    result = new ResultWithError<X>();
                    result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method CreateWithError"));
                }
                return result;
            }
            ResultWithError<X> error = new ResultWithError<X>();
            error.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide a value to create"));
            return error;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public X? Create<X>(X value) where X : U
        {
            ResultWithError<X> result = CreateWithError(value);
            if (result.Success)
            {
                return result.Result;
            }
            return default;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        X IGenericDM.Create<X>(X value)
        {
            X? result = InvokeMethod<X, X>(new object[] { value });
            if (result == null)
            {
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
                return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
            }
            return result;
        }
        #endregion

        #endregion

        #region Update

        #region List
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<List<X>> UpdateWithError<X>(List<X> values) where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.UpdateWithError<X>(List<X> values)
        {
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(new object[] { values });
            if (result == null)
            {
                result = new ResultWithError<List<X>>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method UpdateWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public List<X> Update<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = UpdateWithError(values);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<X>();
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        List<X> IGenericDM.Update<X>(List<X> values)
        {
            List<X>? result = InvokeMethod<List<X>, X>(new object[] { values });
            if (result == null)
            {
                return new List<X>();
            }
            return result;
        }
        #endregion

        #region Item
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> UpdateWithError<X>(X value) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();
            ResultWithError<List<X>> resultList = UpdateWithError(new List<X>() { value });
            result.Errors = resultList.Errors;
            if (resultList.Result?.Count > 0)
            {
                result.Result = resultList.Result[0];
            }
            else
            {
                result.Result = default;
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<X> IGenericDM.UpdateWithError<X>(X value)
        {
            ResultWithError<X>? result = InvokeMethod<ResultWithError<X>, X>(new object[] { value });
            if (result == null)
            {
                result = new ResultWithError<X>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method UpdateWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public X? Update<X>(X value) where X : U
        {
            ResultWithError<X> result = UpdateWithError(value);
            if (result.Success)
            {
                return result.Result;
            }
            return default;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        X IGenericDM.Update<X>(X value)
        {
            X? result = InvokeMethod<X, X>(new object[] { value });
            if (result == null)
            {
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
                return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
            }
            return result;
        }
        #endregion

        #endregion

        #region Delete

        #region List
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<List<X>> DeleteWithError<X>(List<X> values) where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.DeleteWithError<X>(List<X> values)
        {
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(new object[] { values });
            if (result == null)
            {
                result = new ResultWithError<List<X>>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method DeleteWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public List<X> Delete<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = DeleteWithError(values);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return new List<X>();
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        List<X> IGenericDM.Delete<X>(List<X> values)
        {
            List<X>? result = InvokeMethod<List<X>, X>(new object[] { values });
            if (result == null)
            {
                return new List<X>();
            }
            return result;
        }
        #endregion

        #region Item
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> DeleteWithError<X>(X value) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();
            ResultWithError<List<X>> resultList = DeleteWithError(new List<X>() { value });
            result.Errors = resultList.Errors;
            if (resultList.Result?.Count > 0)
            {
                result.Result = resultList.Result[0];
            }
            else
            {
                result.Result = default;
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<X> IGenericDM.DeleteWithError<X>(X value)
        {
            ResultWithError<X>? result = InvokeMethod<ResultWithError<X>, X>(new object[] { value });
            if (result == null)
            {
                result = new ResultWithError<X>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method DeleteWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public X? Delete<X>(X value) where X : U
        {
            ResultWithError<X> result = DeleteWithError(value);
            if (result.Success)
            {
                return result.Result;
            }
            return default;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        X IGenericDM.Delete<X>(X value)
        {
            X? result = InvokeMethod<X, X>(new object[] { value });
            if (result == null)
            {
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
                return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
            }
            return result;
        }
        #endregion

        #endregion

        #region Utils
        protected X? InvokeMethod<X, Y>(object[]? parameters = null, bool checkSameParam = true, [CallerMemberName] string name = "")
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
                    try
                    {
                        MethodInfo methodType = method.MakeGenericMethod(typeof(Y));
                        if (checkSameParam)
                        {
                            if (IsSameParameters(methodType.GetParameters(), types))
                            {
                                return (X?)methodType.Invoke(this, parameters);
                            }
                        }
                        else
                        {
                            return (X?)methodType.Invoke(this, parameters);
                        }
                    }
                    catch
                    {
                        // it ll fail if Generic constraint are different but we can't deal it properly inside code so let the compiler do the job
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
                    Type paramType = parameterInfos[i].ParameterType;
                    if (paramType.IsInterface)
                    {
                        if (!types[i].GetInterfaces().Contains(paramType))
                        {
                            return false;
                        }
                    }
                    else if (parameterInfos[i].ParameterType != types[i])
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
