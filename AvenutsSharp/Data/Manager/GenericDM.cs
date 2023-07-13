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
        private static readonly Dictionary<Type, IGenericDM> dico = new();


        public static IGenericDM Get<U>() where U : IStorable
        {
            return Get(typeof(U));
        }
        public static IGenericDM Get(Type U)
        {
            if (dico.ContainsKey(U))
            {
                return dico[U];
            }
            throw new DataError(DataErrorCode.DMNotExist, "Can't found a data manger for type " + U.Name).GetException();
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
        private static readonly Mutex mutexGetInstance = new();
        private static readonly Dictionary<Type, T> instances = new();
        ///// <summary>
        ///// Singleton pattern
        ///// </summary>
        ///// <returns></returns>
        public static T GetInstance()
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
        public Type GetMainType()
        {
            return typeof(U);
        }
        public virtual List<Type> DefineManualDependances()
        {
            return new List<Type>();
        }
        public string Name
        {
            get => GetType().Name.Split('`')[0] + "<" + typeof(U).Name.Split('`')[0] + ">";
        }
        public bool IsInit { get; protected set; }
        #endregion

        protected PyramidInfo PyramidInfo { get; set; }

        private Dictionary<Type, PyramidInfo> PyramidsInfo { get; set; } = new Dictionary<Type, PyramidInfo>();
        protected Type? RootType { get; set; }
        protected DataManagerConfig? Config { get; set; }

#pragma warning disable CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
        protected GenericDM()
#pragma warning restore CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
        {
        }

        #region Config
        public virtual Task<bool> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            PyramidInfo = pyramid;
            PyramidsInfo[pyramid.type] = pyramid;
            if (pyramid.aliasType != null)
            {
                PyramidsInfo[pyramid.aliasType] = pyramid;
            }
            this.Config = config;
            SetDMForType(pyramid, true);
            return Task.FromResult(true);
        }

        private void SetDMForType(PyramidInfo pyramid, bool isRoot)
        {
            if (!pyramid.isForceInherit || !isRoot)
            {
                if (RootType == null)
                {
                    RootType = pyramid.type;
                }
                GenericDM.Set(pyramid.type, this);
                PyramidsInfo[pyramid.type] = pyramid;
                if (pyramid.aliasType != null)
                {
                    GenericDM.Set(pyramid.aliasType, this);
                    PyramidsInfo[pyramid.aliasType] = pyramid;
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
                    IsInit = true;
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
        public abstract IQueryBuilder<X> CreateQuery<X>() where X : U;
        IQueryBuilder<X>? IGenericDM.CreateQuery<X>()
        {
            IQueryBuilder<X>? result = InvokeMethod<IQueryBuilder<X>, X>(Array.Empty<object>());
            return result;
        }
        #endregion

        #region generic update
        public abstract IUpdateBuilder<X> CreateUpdate<X>() where X : U;
        IUpdateBuilder<X>? IGenericDM.CreateUpdate<X>()
        {
            IUpdateBuilder<X>? result = InvokeMethod<IUpdateBuilder<X>, X>(Array.Empty<object>());
            return result;
        }
        #endregion

        #region generic delete
        public abstract IDeleteBuilder<X> CreateDelete<X>() where X : U;
        IDeleteBuilder<X>? IGenericDM.CreateDelete<X>()
        {
            IDeleteBuilder<X>? result = InvokeMethod<IDeleteBuilder<X>, X>(Array.Empty<object>());
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
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(Array.Empty<object>());
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
            List<X>? result = InvokeMethod<List<X>, X>(Array.Empty<object>());
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

        public object? GetById(int id)
        {
            return GetById<U>(id);
        }
        #endregion

        #region GetByIds
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public abstract ResultWithError<List<X>> GetByIdsWithError<X>(List<int> ids) where X : U;
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.GetByIdsWithError<X>(List<int> ids)
        {
            ResultWithError<List<X>>? result = InvokeMethod<ResultWithError<List<X>>, X>(new object[] { ids });
            if (result == null)
            {
                result = new ResultWithError<List<X>>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method GetByIdsWithError"));
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public List<X>? GetByIds<X>(List<int> ids) where X : U
        {
            ResultWithError<List<X>> result = GetByIdsWithError<X>(ids);
            if (result.Success)
            {
                return result.Result;
            }
            return default;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        List<X> IGenericDM.GetByIds<X>(List<int> ids)
        {
            List<X>? result = InvokeMethod<List<X>, X>(new object[] { ids });
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
            ResultWithError<List<X>> result = new();

            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = InvokeMethod<ResultWithError<List<U>>, U>(new object[] { valuesTemp });
            if (resultTemp is IResultWithError resultCasted)
            {
                if (resultCasted.Result is List<U> castedList)
                {
                    result.Result = TransformList<U, X>(castedList);
                }
                else
                {
                    result.Errors = resultCasted.Errors;
                }
            }
            else
            {
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
            List<X> result = new();
            List<U> valuesTemp = TransformList<X, U>(values);
            List<U>? resultTemp = InvokeMethod<List<U>, U>(new object[] { valuesTemp });
            if (resultTemp != null)
            {
                return TransformList<U, X>(resultTemp);
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
            ResultWithError<X> result = new();
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
            ResultWithError<X> result = new();
            if (value is U)
            {
                ResultWithError<U>? resultTemp = InvokeMethod<ResultWithError<U>, U>(new object[] { value });
                if (resultTemp is IResultWithError resultCasted)
                {
                    if (resultCasted.Result is X casted)
                    {
                        result.Result = casted;
                    }
                    else
                    {
                        result.Errors = resultCasted.Errors;
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method CreateWithError"));
                }
                return result;
            }
            result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide a value to create"));
            return result;
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
            if (value is U)
            {
                U? result = InvokeMethod<U, U>(new object[] { value });
                if(result is X resultCasted)
                {
                    return resultCasted;
                }
            }
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
            return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.

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
            ResultWithError<List<X>> result = new();
            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = InvokeMethod<ResultWithError<List<U>>, U>(new object[] { valuesTemp });
            if (resultTemp is IResultWithError resultWithError)
            {
                if(resultWithError.Result is List<U> castedList)
                {
                    result.Result = TransformList<U, X>(castedList);
                }
                else
                {
                    result.Errors = resultWithError.Errors;
                }
            }
            else
            {
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
            List<U> valuesTemp = TransformList<X, U>(values);
            List<U>? result = InvokeMethod<List<U>, U>(new object[] { valuesTemp });
            if (result != null)
            {
                return TransformList<U, X>(result);
            }
            return new List<X>();
        }
        #endregion

        #region Item
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> UpdateWithError<X>(X value) where X : U
        {
            ResultWithError<X> result = new();
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
            ResultWithError<X> result = new();
            if (value is U)
            {
                ResultWithError<U>? resultTemp = InvokeMethod<ResultWithError<U>, U>(new object[] { value });
                if (resultTemp is IResultWithError resultWithError)
                {
                    if(resultWithError.Result is X castedItem)
                    {
                        result.Result = castedItem;
                    }
                    else
                    {
                        result.Errors = resultWithError.Errors;
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method UpdateWithError"));
                }
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
            if (value is U)
            {
                U? result = InvokeMethod<U, U>(new object[] { value });
                if (result is X casted)
                {
                    return casted;
                }
            }
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
            return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
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
            ResultWithError<List<X>> result = new();
            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = InvokeMethod<ResultWithError<List<U>>, U>(new object[] { valuesTemp });
            if (resultTemp is IResultWithError resultWithError)
            {
                if (resultWithError.Result is List<U> castedList)
                {
                    result.Result = TransformList<U, X>(castedList);
                }
                else
                {
                    result.Errors = resultWithError.Errors;
                }
            }
            else
            {
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
            List<U> valuesTemp = TransformList<X, U>(values);
            List<U>? result = InvokeMethod<List<U>, U>(new object[] { valuesTemp });
            if (result != null)
            {
                return TransformList<U, X>(result);
            }
            return new List<X>();
        }
        #endregion

        #region Item
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> DeleteWithError<X>(X value) where X : U
        {
            ResultWithError<X> result = new();
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
            ResultWithError<X> result = new();
            if (value is U)
            {
                ResultWithError<U>? resultTemp = InvokeMethod<ResultWithError<U>, U>(new object[] { value });
                if (resultTemp is IResultWithError resultWithError)
                {
                    if (resultWithError.Result is X castedItem)
                    {
                        result.Result = castedItem;
                    }
                    else
                    {
                        result.Errors = resultWithError.Errors;
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method DeleteWithError"));
                }
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
            if (value is U)
            {
                U? result = InvokeMethod<U, U>(new object[] { value });
                if (result is X casted)
                {
                    return casted;
                }
            }
#pragma warning disable CS8603 // Existence possible d'un retour de référence null.
            return default;
#pragma warning restore CS8603 // Existence possible d'un retour de référence null.
        }
        #endregion

        #endregion

        #region Utils
        protected List<Y> TransformList<X, Y>(List<X> input)
        {
            List<Y> result = new();
            foreach(X item in input)
            {
                if(item is Y casted)
                {
                    result.Add(casted);
                }
            }
            return result;
        }
        protected X? InvokeMethod<X, Y>(object[]? parameters = null, bool checkSameParam = true, [CallerMemberName] string name = "")
        {
            parameters ??= Array.Empty<object>();
            List<Type> types = new();
            foreach (object param in parameters)
            {
                Type type = param.GetType();
                if(param is Expression exp && type.IsGenericType)
                {
                    Type[] t = exp.Type.GetGenericArguments();
                    Type fctType = t.Length switch
                    {
                        1 => typeof(Func<>),
                        2 => typeof(Func<,>),
                        _ => throw new NotImplementedException()
                    };
                    fctType = fctType.MakeGenericType(t);
                    type = typeof(Expression<>).MakeGenericType(fctType);
                }
                types.Add(type);
            }

            MethodInfo[] methods = this.GetType().GetMethods();
            foreach (MethodInfo method in methods)
            {
                if (method.Name == name && method.IsGenericMethod)
                {
                    try
                    {
                        Type YType = typeof(Y);
                        MethodInfo methodType = method.MakeGenericMethod(YType);
                        if (checkSameParam)
                        {
                            if (GenericDM<T, U>.IsSameParameters(methodType.GetParameters(), types))
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
        private static bool IsSameParameters(ParameterInfo[] parameterInfos, List<Type> types)
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
