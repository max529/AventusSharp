using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB
{
    public class DatabaseDMSimple<T> : DatabaseDM<DatabaseDMSimple<T>, T> where T : IStorable { }
    public class DatabaseDM<T, U> : GenericDM<T, U> where T : IGenericDM<U>, new() where U : IStorable
    {
        private Dictionary<int, U> records { get; } = new Dictionary<int, U>();

        public bool useLocalCache { get; private set; } = false;

        protected IStorage? storage;
        protected List<Type> getAllDone { get; } = new List<Type>();

        protected IStorage Storage
        {
            get
            {
                if (storage != null)
                {
                    return storage;
                }
                throw new DataError(DataErrorCode.StorageNotFound, "You must define a storage inside your DM " + GetType().Name).GetException();
            }
        }

        #region Config
        protected virtual IStorage? DefineStorage()
        {
            return null;
        }
        protected virtual bool? UseLocalCache()
        {
            return null;
        }
        public override async Task<bool> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            storage = DefineStorage();
            if (storage == null)
            {
                storage = config.defaultStorage;
            }
            if (storage == null)
            {
                return false;
            }
            bool? localCacheTemp = UseLocalCache();
            if (localCacheTemp == null)
            {
                useLocalCache = config.preferLocalCache;
            }
            else
            {
                useLocalCache = (bool)localCacheTemp;
            }
            if (!storage.IsConnectedOneTime)
            {
                if (!storage.Connect())
                {
                    return false;
                }
            }
            storage.AddPyramid(pyramid);
            return await base.SetConfiguration(pyramid, config);

        }
        protected override Task<bool> Initialize()
        {
            if (storage != null)
            {
                storage.CreateLinks();
                VoidWithError result = storage.CreateTable(pyramidInfo);
                if (result.Success)
                {
                    return Task.FromResult(true);
                }

                foreach (DataError error in result.Errors)
                {
                    error.Print();
                }
                return Task.FromResult(false);
            }
            new DataError(DataErrorCode.StorageNotFound, "You must define a storage inside your DM " + GetType().Name).Print();
            return Task.FromResult(false);
        }


        #endregion

        #region Get
        public override QueryBuilder<X> CreateQuery<X>()
        {
            return new DatabaseQueryBuilder<X>(Storage);
        }

        private Dictionary<Type, object> savedGetAllQuery = new Dictionary<Type, object>();
        public override ResultWithError<List<X>> GetAllWithError<X>()
        {
            if (useLocalCache)
            {
                return GetAllWithErrorCache<X>();
            }
            return GetAllWithErrorNoCache<X>();
        }
        public ResultWithError<List<X>> GetAllWithErrorCache<X>() where X : U
        {
            Type type = typeof(X);
            Type rootType = typeof(U);
            if (getAllDone.Contains(rootType) || getAllDone.Contains(type))
            {
                ResultWithError<List<X>> result = new ResultWithError<List<X>>();
                result.Result = new List<X>();
                foreach (KeyValuePair<int, U> record in records)
                {
                    if (record is X casted)
                    {
                        result.Result.Add(casted);
                    }
                }
                return result;
            }
            ResultWithError<List<X>> resultNoCache = GetAllWithErrorNoCache<X>();
            if (resultNoCache.Success && resultNoCache.Result != null)
            {
                foreach (X newRecord in resultNoCache.Result)
                {
                    if (!records.ContainsKey(newRecord.id))
                    {
                        records.Add(newRecord.id, newRecord);
                    }
                }
                getAllDone.Add(type);
            }
            return resultNoCache;
        }
        public ResultWithError<List<X>> GetAllWithErrorNoCache<X>() where X : U
        {
            Type x = typeof(X);
            if (!savedGetAllQuery.ContainsKey(x))
            {
                savedGetAllQuery[x] = new DatabaseQueryBuilder<X>(Storage);
            }
            return ((DatabaseQueryBuilder<X>)savedGetAllQuery[x]).RunWithError();
        }


        private Dictionary<Type, object> savedGetByIdQuery = new Dictionary<Type, object>();
        public override ResultWithError<X> GetByIdWithError<X>(int id)
        {
            if (useLocalCache)
            {
                return GetByIdWithErrorCache<X>(id);
            }
            return GetByIdWithErrorNoCache<X>(id);
        }

        public ResultWithError<X> GetByIdWithErrorCache<X>(int id) where X : U
        {
            if (records.ContainsKey(id) && records[id] is X casted)
            {
                ResultWithError<X> result = new ResultWithError<X>();
                result.Result = casted;
                return result;
            }
            ResultWithError<X> resultNoCache = GetByIdWithErrorNoCache<X>(id);
            if (resultNoCache.Success && resultNoCache.Result != null)
            {
                records[resultNoCache.Result.id] = resultNoCache.Result;
            }
            return resultNoCache;
        }
        public ResultWithError<X> GetByIdWithErrorNoCache<X>(int id) where X : U
        {
            ResultWithError<X> result = new ResultWithError<X>();

            Type x = typeof(X);
            if (!savedGetByIdQuery.ContainsKey(x))
            {
                DatabaseQueryBuilder<X> queryBuilderTemp = new DatabaseQueryBuilder<X>(Storage);
                queryBuilderTemp.WhereWithParameters(i => i.id == id);
                savedGetByIdQuery[x] = queryBuilderTemp;
            }

            DatabaseQueryBuilder<X> queryBuilder = (DatabaseQueryBuilder<X>)savedGetByIdQuery[x];
            queryBuilder.SetVariable("id", id);
            ResultWithError<List<X>> resultTemp = queryBuilder.RunWithError();

            if (resultTemp.Success)
            {
                if (resultTemp.Result?.Count == 1)
                {
                    result.Result = resultTemp.Result[0];
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.ItemNoExistInsideStorage, "The item " + id + " can't be found inside the storage"));
                }
            }
            else
            {
                result.Errors.AddRange(resultTemp.Errors);
            }
            return result;
        }

        public override ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func)
        {
            if (useLocalCache)
            {
                return WhereWithErrorCache(func);
            }
            return WhereWithErrorNoCache(func);
        }

        public ResultWithError<List<X>> WhereWithErrorCache<X>(Expression<Func<X, bool>> func) where X : U
        {
            Type type = typeof(X);
            Type rootType = typeof(U);
            if (getAllDone.Contains(rootType) || getAllDone.Contains(type))
            {
                ResultWithError<List<X>> result = new ResultWithError<List<X>>();
                result.Result = new List<X>();
                Func<X, bool> fct = func.Compile();
                foreach (KeyValuePair<int, U> pair in records)
                {
                    try
                    {
                        if (pair.Value is X casted)
                        {
                            if (fct.Invoke(casted))
                            {
                                result.Result.Add(casted);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                    }
                }
                return result;
            }
            return WhereWithErrorNoCache(func);
        }

        public ResultWithError<List<X>> WhereWithErrorNoCache<X>(Expression<Func<X, bool>> func) where X : U
        {
            DatabaseQueryBuilder<X> queryBuilder = new DatabaseQueryBuilder<X>(Storage);
            queryBuilder.Where(func);
            return queryBuilder.RunWithError();
        }
        #endregion

        #region Create
        public override ResultWithError<List<X>> CreateWithError<X>(List<X> values)
        {
            ResultWithError<List<X>> result = Storage.Create(values);
            if (useLocalCache && result.Success && result.Result != null)
            {
                foreach (X value in result.Result)
                {
                    if (!records.ContainsKey(value.id))
                    {
                        records[value.id] = value;
                    }
                }
            }
            return result;
        }
        #endregion

        #region Update
        public override UpdateBuilder<X> CreateUpdate<X>()
        {
            return new DatabaseUpdateBuilder<X>(Storage);
        }
        public override ResultWithError<List<X>> UpdateWithError<X>(List<X> values)
        {
            if (useLocalCache)
            {
                return UpdateWithErrorCache(values);
            }
            return UpdateWithErrorNoCache(values);
        }
        public ResultWithError<List<X>> UpdateWithErrorCache<X>(List<X> values) where X : U
        {
            ResultWithError<List<X>> resultTemp = new ResultWithError<List<X>>();

            List<X> oldValues = new List<X>();
            foreach (X value in values)
            {
                if (records.ContainsKey(value.id) && records[value.id] is X casted)
                {
                    oldValues.Add(casted);
                }
                else
                {
                    resultTemp.Errors.Add(new DataError(DataErrorCode.ItemNoExistInsideStorage, "Can't find a " + value.GetType().Name + " with id " + value.id + ""));
                }
            }
            if (!resultTemp.Success)
            {
                return resultTemp;
            }
            return Storage.Update(values, oldValues);
        }
        public ResultWithError<List<X>> UpdateWithErrorNoCache<X>(List<X> values) where X : U
        {
            return Storage.Update(values, null);
        }
        #endregion

        #region Delete
        public override ResultWithError<List<X>> DeleteWithError<X>(List<X> values)
        {
            ResultWithError<List<X>> result = Storage.Delete(values);
            if (useLocalCache && result.Success && result.Result != null)
            {
                foreach (X value in result.Result)
                {
                    if (records.ContainsKey(value.id))
                    {
                        records.Remove(value.id);
                    }
                }
            }
            return result;
        }


        #endregion

    }
}
