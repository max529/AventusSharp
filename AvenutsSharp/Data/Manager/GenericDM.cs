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
        private static readonly List<IGenericDM> dms = new();

        public static List<Type> GetExistingDMTypes()
        {
            return dms.Select(v => v.GetType()).ToList();
        }
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
        public static ResultWithError<IGenericDM> GetWithError<U>() where U : IStorable
        {
            return GetWithError(typeof(U));
        }
        public static ResultWithError<IGenericDM> GetWithError(Type U)
        {
            ResultWithError<IGenericDM> result = new ResultWithError<IGenericDM>();
            if (dico.ContainsKey(U))
            {
                result.Result = dico[U];
                return result;
            }
            result.Errors.Add(new DataError(DataErrorCode.DMNotExist, "Can't found a data manger for type " + U.Name));
            return result;
        }
        public static VoidWithError Set(Type type, IGenericDM manager)
        {
            VoidWithError result = new VoidWithError();
            if (dico.ContainsKey(type))
            {
                if (dico[type] != manager)
                {
                    result.Errors.Add(new DataError(DataErrorCode.DMAlreadyExist, "A manager already exists for type " + type.Name));
                }
            }
            else
            {
                if (!dms.Contains(manager))
                {
                    dms.Add(manager);
                }
                dico[type] = manager;
            }
            return result;

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
                T dm = new T();
                instances.Add(typeof(T), dm);
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
        public virtual Task<VoidWithError> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            VoidWithError result = new VoidWithError();
            PyramidInfo = pyramid;
            PyramidsInfo[pyramid.type] = pyramid;
            if (pyramid.aliasType != null)
            {
                PyramidsInfo[pyramid.aliasType] = pyramid;
            }
            this.Config = config;
            result = SetDMForType(pyramid, true);
            return Task.FromResult(result);
        }

        private VoidWithError SetDMForType(PyramidInfo pyramid, bool isRoot)
        {
            VoidWithError result = new();
            if (!pyramid.isForceInherit || !isRoot)
            {
                isRoot = false;
                if (RootType == null)
                {
                    RootType = pyramid.type;
                }
                VoidWithError resultTemp = GenericDM.Set(pyramid.type, this);
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                PyramidsInfo[pyramid.type] = pyramid;
                if (pyramid.aliasType != null)
                {
                    resultTemp = GenericDM.Set(pyramid.aliasType, this);
                    if (!resultTemp.Success)
                    {
                        return resultTemp;
                    }
                    PyramidsInfo[pyramid.aliasType] = pyramid;
                }
            }
            foreach (PyramidInfo child in pyramid.children)
            {
                VoidWithError resultTemp = SetDMForType(child, isRoot);
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
            }
            return result;
        }

        public async Task<VoidWithError> Init()
        {
            VoidWithError result = new();
            try
            {
                result = await Initialize();
                if (result.Success)
                {
                    IsInit = true;
                }
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
            return result;
        }
        protected abstract Task<VoidWithError> Initialize();
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
        protected abstract ResultWithError<List<X>> GetAllLogic<X>() where X : U;

        protected virtual List<DataError> CanGetAll()
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeGetAll()
        {
            try
            {
                BeforeGetAll();
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeGetAll()
        {
        }
        private DataError? WrapperAfterGetAll<X>(ResultWithError<List<X>> result) where X : U
        {
            try
            {
                AfterGetAll(result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterGetAll<X>(ResultWithError<List<X>> result) where X : U
        {
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> GetAllWithError<X>() where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<DataError> errors = CanGetAll();
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeGetAll();
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = GetAllLogic<X>();
            error = WrapperAfterGetAll(result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
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
        protected abstract ResultWithError<X> GetByIdLogic<X>(int id) where X : U;

        protected virtual List<DataError> CanGetById(int id)
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeGetById(int id)
        {
            try
            {
                BeforeGetById(id);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeGetById(int id)
        {
        }
        private DataError? WrapperAfterGetById<X>(int id, ResultWithError<X> result) where X : U
        {
            try
            {
                AfterGetById(id, result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterGetById<X>(int id, ResultWithError<X> result) where X : U
        {
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> GetByIdWithError<X>(int id) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();
            List<DataError> errors = CanGetById(id);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeGetById(id);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = GetByIdLogic<X>(id);
            error = WrapperAfterGetById(id, result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
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
        protected abstract ResultWithError<List<X>> GetByIdsLogic<X>(List<int> ids) where X : U;

        protected virtual List<DataError> CanGetByIds(List<int> ids)
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeGetByIds(List<int> ids)
        {
            try
            {
                BeforeGetByIds(ids);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeGetByIds(List<int> ids)
        {
        }
        private DataError? WrapperAfterGetByIds<X>(List<int> ids, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                AfterGetByIds(ids, result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterGetByIds<X>(List<int> ids, ResultWithError<List<X>> result) where X : U
        {
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> GetByIdsWithError<X>(List<int> ids) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<DataError> errors = CanGetByIds(ids);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeGetByIds(ids);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = GetByIdsLogic<X>(ids);
            error = WrapperAfterGetByIds(ids, result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
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
        protected abstract ResultWithError<List<X>> WhereLogic<X>(Expression<Func<X, bool>> func) where X : U;

        protected virtual List<DataError> CanWhere<X>(Expression<Func<X, bool>> func) where X : U
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeWhere<X>(Expression<Func<X, bool>> func) where X : U
        {
            try
            {
                BeforeWhere(func);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeWhere<X>(Expression<Func<X, bool>> func) where X : U
        {
        }
        private DataError? WrapperAfterWhere<X>(Expression<Func<X, bool>> func, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                AfterWhere(func, result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterWhere<X>(Expression<Func<X, bool>> func, ResultWithError<List<X>> result) where X : U
        {
        }


        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<DataError> errors = CanWhere(func);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeWhere(func);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = WhereLogic(func);
            error = WrapperAfterWhere(func, result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
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
        protected abstract ResultWithError<List<X>> CreateLogic<X>(List<X> values) where X : U;
        protected virtual List<DataError> CanCreate<X>(List<X> values) where X : U
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeCreate<X>(List<X> values) where X : U
        {
            try
            {
                BeforeCreate(values);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeCreate<X>(List<X> values) where X : U
        {
        }
        private DataError? WrapperAfterCreate<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                AfterCreate(values, result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterCreate<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
        }


        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> CreateWithError<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<DataError> errors = CanCreate(values);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeCreate(values);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = CreateLogic(values);
            error = WrapperAfterCreate(values, result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.CreateWithError<X>(List<X> values)
        {
            ResultWithError<List<X>> result = new();

            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = InvokeMethod<ResultWithError<List<U>>, U>(new object[] { valuesTemp });
            if (resultTemp != null)
            {
                if (resultTemp.Result is List<U> castedList)
                {
                    result.Result = TransformList<U, X>(castedList);
                }
                else
                {
                    result.Errors = resultTemp.Errors;
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
                if (resultTemp != null)
                {
                    if (resultTemp.Result is X casted)
                    {
                        result.Result = casted;
                    }
                    else
                    {
                        result.Errors = resultTemp.Errors;
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
                if (result is X resultCasted)
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
        protected abstract ResultWithError<List<X>> UpdateLogic<X>(List<X> values) where X : U;
        protected virtual List<DataError> CanUpdate<X>(List<X> values) where X : U
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeUpdate<X>(List<X> values) where X : U
        {
            try
            {
                BeforeUpdate(values);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeUpdate<X>(List<X> values) where X : U
        {
        }
        private DataError? WrapperAfterUpdate<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                AfterUpdate(values, result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterUpdate<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
        }


        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> UpdateWithError<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<DataError> errors = CanUpdate(values);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeUpdate(values);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = UpdateLogic(values);
            error = WrapperAfterUpdate(values, result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.UpdateWithError<X>(List<X> values)
        {
            ResultWithError<List<X>> result = new();
            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = InvokeMethod<ResultWithError<List<U>>, U>(new object[] { valuesTemp });
            if (resultTemp != null)
            {
                if (resultTemp.Result is List<U> castedList)
                {
                    result.Result = TransformList<U, X>(castedList);
                }
                else
                {
                    result.Errors = resultTemp.Errors;
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
                if (resultTemp != null)
                {
                    if (resultTemp.Result is X castedItem)
                    {
                        result.Result = castedItem;
                    }
                    else
                    {
                        result.Errors = resultTemp.Errors;
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
        protected abstract ResultWithError<List<X>> DeleteLogic<X>(List<X> values) where X : U;
        protected virtual List<DataError> CanDelete<X>(List<X> values) where X : U
        {
            return new List<DataError>();
        }
        private DataError? WrapperBeforeDelete<X>(List<X> values) where X : U
        {
            try
            {
                BeforeDelete(values);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void BeforeDelete<X>(List<X> values) where X : U
        {
        }
        private DataError? WrapperAfterDelete<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                AfterDelete(values, result);
                return null;
            }
            catch (Exception e)
            {
                return new DataError(DataErrorCode.UnknowError, e);
            }
        }
        protected virtual void AfterDelete<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> DeleteWithError<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<DataError> errors = CanDelete(values);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            DataError? error = WrapperBeforeDelete(values);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            result = DeleteLogic(values);
            error = WrapperAfterDelete(values, result);
            if (error != null)
            {
                result.Errors.Add(error);
                return result;
            }
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.DeleteWithError<X>(List<X> values)
        {
            ResultWithError<List<X>> result = new();
            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = InvokeMethod<ResultWithError<List<U>>, U>(new object[] { valuesTemp });
            if (resultTemp !=null)
            {
                if (resultTemp.Result is List<U> castedList)
                {
                    result.Result = TransformList<U, X>(castedList);
                }
                else
                {
                    result.Errors = resultTemp.Errors;
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
                if (resultTemp != null)
                {
                    if (resultTemp.Result is X castedItem)
                    {
                        result.Result = castedItem;
                    }
                    else
                    {
                        result.Errors = resultTemp.Errors;
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
            foreach (X item in input)
            {
                if (item is Y casted)
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
                if (param is Expression exp && type.IsGenericType)
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
