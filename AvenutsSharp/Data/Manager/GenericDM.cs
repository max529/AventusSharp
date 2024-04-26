using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using MySqlX.XDevAPI.Common;
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
        public static VoidWithDataError Set(Type type, IGenericDM manager)
        {
            VoidWithDataError result = new VoidWithDataError();
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

        protected bool printErrorInConsole { get; set; }
        #endregion

        protected PyramidInfo PyramidInfo { get; set; }

        private Dictionary<Type, PyramidInfo> PyramidsInfo = new Dictionary<Type, PyramidInfo>();
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
            bool? printError = MustPrintErrorInConsole();
            if (printError != null)
            {
                printErrorInConsole = (bool)printError;
            }
            else
            {
                printErrorInConsole = config.log.printErrorInConsole;
            }
            result = SetDMForType(pyramid, true).ToGeneric();
            return Task.FromResult(result);
        }

        private VoidWithDataError SetDMForType(PyramidInfo pyramid, bool isRoot)
        {
            VoidWithDataError result = new();
            if ((!pyramid.isForceInherit && !pyramid.nonGenericExtension) || !isRoot)
            {
                isRoot = false;
                if (RootType == null)
                {
                    RootType = pyramid.type;
                }
                VoidWithDataError resultTemp = GenericDM.Set(pyramid.type, this);
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
                VoidWithDataError resultTemp = SetDMForType(child, isRoot);
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

        protected bool? MustPrintErrorInConsole()
        {
            return null;
        }
        #endregion

        #region generic query
        public abstract IQueryBuilder<X> CreateQuery<X>() where X : U;
        IQueryBuilder<X> IGenericDM.CreateQuery<X>()
        {
            IQueryBuilder<X>? result = InvokeMethod<IQueryBuilder<X>, X>(Array.Empty<object>());
            if (result == null)
            {
                throw new Exception("Impossible");
            }
            return result;
        }


        #endregion

        #region generic exist
        public abstract IExistBuilder<X> CreateExist<X>() where X : U;
        IExistBuilder<X> IGenericDM.CreateExist<X>()
        {
            IExistBuilder<X>? result = InvokeMethod<IExistBuilder<X>, X>(Array.Empty<object>());
            if (result == null)
            {
                throw new Exception("Create exist not exist => impossible");
            }
            return result;
        }
        #endregion

        #region generic update
        public abstract IUpdateBuilder<X> CreateUpdate<X>() where X : U;
        IUpdateBuilder<X> IGenericDM.CreateUpdate<X>()
        {
            IUpdateBuilder<X>? result = InvokeMethod<IUpdateBuilder<X>, X>(Array.Empty<object>());
            if (result == null)
            {
                throw new Exception("Create update not exist => impossible");
            }
            return result;
        }
        #endregion

        #region generic delete
        public abstract IDeleteBuilder<X> CreateDelete<X>() where X : U;
        IDeleteBuilder<X> IGenericDM.CreateDelete<X>()
        {
            IDeleteBuilder<X>? result = InvokeMethod<IDeleteBuilder<X>, X>(Array.Empty<object>());
            if (result == null)
            {
                throw new Exception("Create delete not exist => impossible");
            }
            return result;
        }
        #endregion

        #region Get

        #region GetAll
        protected abstract ResultWithError<List<X>> GetAllLogic<X>() where X : U;

        protected virtual List<GenericError> CanGetAll()
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeGetAll(List<GenericError> errors)
        {
            try
            {
                errors.AddRange(BeforeGetAllWithError());
                BeforeGetAll();
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeGetAllWithError()
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeGetAll()
        {
        }
        private void WrapperAfterGetAll<X>(ResultWithError<List<X>> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterGetAllWithError<X>(result));
                AfterGetAll<X>(result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> AfterGetAllWithError<X>(ResultWithError<List<X>> result) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void AfterGetAll<X>(ResultWithError<List<X>> result) where X : U
        {
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<U>> GetAllWithError()
        {
            return GetAllWithError<U>();
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> GetAllWithError<X>() where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<GenericError> errors = CanGetAll();
            if (errors.Count > 0)
            {
                result.Errors = errors;
                PrintErrors(result);
                return result;
            }
            WrapperBeforeGetAll(errors);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                PrintErrors(result);
                return result;
            }
            result = GetAllLogic<X>();
            WrapperAfterGetAll(result);
            PrintErrors(result);
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
        public List<U> GetAll()
        {
            return GetAll<U>();
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

        protected virtual List<GenericError> CanGetById(int id)
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeGetById(int id, List<GenericError> errors)
        {
            try
            {
                errors.AddRange(BeforeGetByIdWithError(id));
                BeforeGetById(id);
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeGetByIdWithError(int id)
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeGetById(int id)
        {
        }
        private void WrapperAfterGetById<X>(int id, ResultWithError<X> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterGetByIdWithError(id, result));
                AfterGetById(id, result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> AfterGetByIdWithError<X>(int id, ResultWithError<X> result) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void AfterGetById<X>(int id, ResultWithError<X> result) where X : U
        {
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<U> GetByIdWithError(int id)
        {
            return GetByIdWithError<U>(id);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<X> GetByIdWithError<X>(int id) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();
            try
            {
                List<GenericError> errors = CanGetById(id);
                if (errors.Count > 0)
                {
                    result.Errors = errors;
                    PrintErrors(result);
                    return result;
                }
                WrapperBeforeGetById(id, errors);
                if (errors.Count > 0)
                {
                    result.Errors = errors;
                    PrintErrors(result);
                    return result;
                }
                result = GetByIdLogic<X>(id);
                WrapperAfterGetById(id, result);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, ex));
            }
            PrintErrors(result);
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
        public U? GetById(int id)
        {
            return GetById<U>(id);
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

        object? IGenericDM.GetById(int id)
        {
            return GetById<U>(id);
        }
        #endregion

        #region GetByIds
        protected abstract ResultWithError<List<X>> GetByIdsLogic<X>(List<int> ids) where X : U;

        protected virtual List<GenericError> CanGetByIds(List<int> ids)
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeGetByIds(List<int> ids, List<GenericError> errors)
        {
            try
            {
                errors.AddRange(BeforeGetByIdsWithError(ids));
                BeforeGetByIds(ids);
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeGetByIdsWithError(List<int> ids)
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeGetByIds(List<int> ids)
        {
        }
        private void WrapperAfterGetByIds<X>(List<int> ids, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterGetByIdsWithError(ids, result));
                AfterGetByIds(ids, result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> AfterGetByIdsWithError<X>(List<int> ids, ResultWithError<List<X>> result) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void AfterGetByIds<X>(List<int> ids, ResultWithError<List<X>> result) where X : U
        {
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<U>> GetByIdsWithError(List<int> ids)
        {
            return GetByIdsWithError<U>(ids);
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> GetByIdsWithError<X>(List<int> ids) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<GenericError> errors = CanGetByIds(ids);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                PrintErrors(result);
                return result;
            }
            WrapperBeforeGetByIds(ids, errors);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                PrintErrors(result);
                return result;
            }
            result = GetByIdsLogic<X>(ids);
            WrapperAfterGetByIds(ids, result);
            PrintErrors(result);
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
        public List<U>? GetByIds(List<int> ids)
        {
            return GetByIds<U>(ids);
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

        protected virtual List<GenericError> CanWhere<X>(Expression<Func<X, bool>> func) where X : U
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeWhere<X>(Expression<Func<X, bool>> func, List<GenericError> errors) where X : U
        {
            try
            {
                errors.AddRange(BeforeWhereWithError(func));
                BeforeWhere(func);
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeWhereWithError<X>(Expression<Func<X, bool>> func) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeWhere<X>(Expression<Func<X, bool>> func) where X : U
        {
        }
        private void WrapperAfterWhere<X>(Expression<Func<X, bool>> func, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterWhereWithError(func, result));
                AfterWhere(func, result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> AfterWhereWithError<X>(Expression<Func<X, bool>> func, ResultWithError<List<X>> result) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void AfterWhere<X>(Expression<Func<X, bool>> func, ResultWithError<List<X>> result) where X : U
        {
        }


        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<U>> WhereWithError(Expression<Func<U, bool>> func)
        {
            return WhereWithError<U>(func);
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func) where X : U
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            List<GenericError> errors = CanWhere(func);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                PrintErrors(result);
                return result;
            }
            WrapperBeforeWhere(func, errors);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                PrintErrors(result);
                return result;
            }
            result = WhereLogic(func);
            WrapperAfterWhere(func, result);
            PrintErrors(result);
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
        public List<U> Where(Expression<Func<U, bool>> func)
        {
            return Where<U>(func);
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

        #region Exist
        public ResultWithError<bool> ExistWithError(Expression<Func<U, bool>> func)
        {
            return CreateExist<U>().Where(func).RunWithError();
        }
        public ResultWithError<bool> ExistWithError<X>(Expression<Func<X, bool>> func) where X : U
        {
            return CreateExist<X>().Where(func).RunWithError();
        }
        ResultWithError<bool> IGenericDM.ExistWithError<X>(Expression<Func<X, bool>> func)
        {
            ResultWithError<bool>? result = InvokeMethod<ResultWithError<bool>, X>(new object[] { func }, false);
            if (result == null)
            {
                result = new ResultWithError<bool>();
                result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Can't found the method WhereWithError"));
            }
            return result;
        }
        public bool Exist(Expression<Func<U, bool>> func)
        {
            return CreateExist<U>().Where(func).Run();
        }
        public bool Exist<X>(Expression<Func<X, bool>> func) where X : U
        {
            return CreateExist<X>().Where(func).Run();
        }
        bool IGenericDM.Exist<X>(Expression<Func<X, bool>> func)
        {
            return InvokeMethod<bool, X>(new object[] { func }, false);
        }
        #endregion

        #region Create

        public event OnCreatedHandler<U> OnCreated;

        #region List
        protected abstract ResultWithError<List<X>> CreateLogic<X>(List<X> values) where X : U;
        protected virtual List<GenericError> CanCreate<X>(List<X> values) where X : U
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeCreate<X>(List<X> values, List<GenericError> errors) where X : U
        {
            try
            {
                errors.AddRange(BeforeCreateWithError(values));
                BeforeCreate(values);
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeCreateWithError<X>(List<X> values) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeCreate<X>(List<X> values) where X : U
        {
        }
        private void WrapperAfterCreate<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterCreateWithError(values, result));
                AfterCreate(values, result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }

        protected virtual List<GenericError> AfterCreateWithError<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            return new List<GenericError>();
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
            List<GenericError> errors = CanCreate(values);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            WrapperBeforeCreate(values, errors);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            result = CreateLogic(values);
            WrapperAfterCreate(values, result);
            OnCreated?.Invoke(TransformResult<X, U>(result));
            return result;
        }
        /// <summary>
        /// <inheritdoc />
        /// </summary>
        ResultWithError<List<X>> IGenericDM.CreateWithError<X>(List<X> values)
        {
            ResultWithError<List<X>> result = new();

            List<U> valuesTemp = TransformList<X, U>(values);
            ResultWithError<List<U>>? resultTemp = CreateWithError(valuesTemp);
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

        public event OnUpdatedHandler<U> OnUpdated;

        #region List
        protected abstract ResultWithError<List<X>> UpdateLogic<X>(List<X> values) where X : U;
        protected virtual List<GenericError> CanUpdate<X>(List<X> values) where X : U
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeUpdate<X>(List<X> values, List<GenericError> errors) where X : U
        {
            try
            {
                errors.AddRange(BeforeUpdateWithError(values));
                BeforeUpdate(values);
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeUpdateWithError<X>(List<X> values) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeUpdate<X>(List<X> values) where X : U
        {
        }
        private void WrapperAfterUpdate<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterUpdateWithError(values, result));
                AfterUpdate(values, result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> AfterUpdateWithError<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            return new List<GenericError>();
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
            List<GenericError> errors = CanUpdate(values);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            WrapperBeforeUpdate(values, errors);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            result = UpdateLogic(values);
            WrapperAfterUpdate(values, result);
            OnUpdated?.Invoke(TransformResult<X, U>(result));
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

        public event OnDeletedHandler<U> OnDeleted;

        #region List
        protected abstract ResultWithError<List<X>> DeleteLogic<X>(List<X> values) where X : U;
        protected virtual List<GenericError> CanDelete<X>(List<X> values) where X : U
        {
            return new List<GenericError>();
        }
        private void WrapperBeforeDelete<X>(List<X> values, List<GenericError> errors) where X : U
        {
            try
            {
                errors.AddRange(BeforeDeleteWithError(values));
                BeforeDelete(values);
            }
            catch (Exception e)
            {
                errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> BeforeDeleteWithError<X>(List<X> values) where X : U
        {
            return new List<GenericError>();
        }
        protected virtual void BeforeDelete<X>(List<X> values) where X : U
        {
        }
        private void WrapperAfterDelete<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            try
            {
                result.Errors.AddRange(AfterDeleteWithError(values, result));
                AfterDelete(values, result);
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
        }
        protected virtual List<GenericError> AfterDeleteWithError<X>(List<X> values, ResultWithError<List<X>> result) where X : U
        {
            return new List<GenericError>();
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
            List<GenericError> errors = CanDelete(values);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            WrapperBeforeDelete(values, errors);
            if (errors.Count > 0)
            {
                result.Errors = errors;
                return result;
            }
            result = DeleteLogic(values);
            WrapperAfterDelete(values, result);
            OnDeleted?.Invoke(TransformResult<X, U>(result));
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
        protected ResultWithError<List<Y>> TransformResult<X, Y>(ResultWithError<List<X>> result)
        {
            ResultWithError<List<Y>> transformed = new ResultWithError<List<Y>>();
            transformed.Errors = result.Errors.ToList();
            if (result.Result != null)
            {
                transformed.Result = TransformList<X, Y>(result.Result);
            }
            return transformed;
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

        protected X? InvokeMethod<X>(Type YType, object[]? parameters = null, bool checkSameParam = true, [CallerMemberName] string name = "")
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

        internal void PrintErrors(IWithError withError)
        {
            if (printErrorInConsole)
            {
                withError.Print();
            }
        }

        void IGenericDM.PrintErrors(IWithError withError)
        {
            PrintErrors(withError);
        }

        #endregion

    }
}
