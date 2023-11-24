using AventusSharp.Data.Manager.DB.Builders;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB
{
    public interface IDatabaseDM
    {
        public bool NeedLocalCache { get; }
        public bool IsShortLink(string path);
        public IDBStorage Storage { get; }
        public List<X> RemoveRecordsItems<X>(List<int> ids) where X : IStorable;
        public List<X> RemoveRecordsItems<X>(List<X> items) where X : IStorable;
    }

    public class SimpleDatabaseDM<U> : DatabaseDM<SimpleDatabaseDM<U>, U> where U : IStorable
    {
    }
    public class DatabaseDM<T, U> : GenericDatabaseDM<T, U> where T : IGenericDM<U>, new() where U : IStorable
    {
        public override sealed Task<VoidWithDataError> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            return base.SetConfiguration(pyramid, config);
        }

        public override sealed IDeleteBuilder<X> CreateDelete<X>()
        {
            return base.CreateDelete<X>();
        }

        public override sealed IQueryBuilder<X> CreateQuery<X>()
        {
            return base.CreateQuery<X>();
        }
        public override sealed IUpdateBuilder<X> CreateUpdate<X>()
        {
            return base.CreateUpdate<X>();
        }
        protected override sealed ResultWithDataError<List<X>> CreateLogic<X>(List<X> values)
        {
            return base.CreateLogic(values);
        }

        protected override sealed ResultWithDataError<List<X>> DeleteLogic<X>(List<X> values)
        {
            return base.DeleteLogic(values);
        }

        protected override sealed ResultWithDataError<List<X>> GetAllLogic<X>()
        {
            return base.GetAllLogic<X>();
        }
        protected override sealed ResultWithDataError<List<X>> GetByIdsLogic<X>(List<int> ids)
        {
            return base.GetByIdsLogic<X>(ids);
        }
        protected override sealed ResultWithDataError<X> GetByIdLogic<X>(int id)
        {
            return base.GetByIdLogic<X>(id);
        }

        protected override sealed ResultWithDataError<List<X>> UpdateLogic<X>(List<X> values)
        {
            return base.UpdateLogic(values);
        }

        protected override sealed ResultWithDataError<List<X>> WhereLogic<X>(Expression<Func<X, bool>> func)
        {
            return base.WhereLogic(func);
        }
    }
    public class GenericDatabaseDM<T, U> : GenericDM<T, U>, IDatabaseDM where T : IGenericDM<U>, new() where U : IStorable
    {
        private readonly Dictionary<int, U> Records = new Dictionary<int, U>();

        public bool NeedLocalCache { get; private set; } = false;
        public bool NeedShortLink { get; private set; } = false;
        public List<string>? ShortLinks { get; private set; } = null;

        protected IDBStorage? storage;
        protected List<Type> GetAllDone { get; } = new List<Type>();


        public IDBStorage Storage
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
        protected virtual IDBStorage? DefineStorage()
        {
            return null;
        }
        protected virtual bool? UseLocalCache()
        {
            return null;
        }
        protected virtual bool? UseShortLink()
        {
            return null;
        }
        protected void ShortLink<X>(Expression<Func<X, IStorable>> fct) where X : U
        {
            ShortLinks ??= new List<string>();
            ShortLinks.Add(LambdaToPath.Translate(fct));
        }
        /// <summary>
        /// Call the metho ShortLink to define which table will be in shortlink (only id inside object)
        /// </summary>
        /// <typeparam name="X"></typeparam>
        protected virtual void DefineShortLinks<X>() where X : U
        {
        }

        public bool IsShortLink(string path)
        {
            if (ShortLinks == null)
            {
                return NeedShortLink;
            }
            return ShortLinks.Contains(path);
        }
        public override async Task<VoidWithDataError> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            VoidWithDataError result = new VoidWithDataError();
            storage = DefineStorage();
            storage ??= config.defaultStorage;
            if (storage == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.StorageNotFound, "Can't found a storage for " + Name));
                return result;
            }
            bool? localCacheTemp = UseLocalCache();
            if (localCacheTemp == null)
            {
                NeedLocalCache = config.preferLocalCache;
            }
            else
            {
                NeedLocalCache = (bool)localCacheTemp;
            }
            DefineShortLinks<U>();
            bool? shortLinkTemp = UseShortLink();
            if (shortLinkTemp == null)
            {
                NeedShortLink = config.preferShortLink;
            }
            else
            {
                NeedShortLink = (bool)shortLinkTemp;
            }
            if (!storage.IsConnectedOneTime)
            {
                VoidWithDataError resultTemp = storage.ConnectWithError();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
            }
            storage.AddPyramid(pyramid);
            return await base.SetConfiguration(pyramid, config);

        }
        protected override Task<VoidWithDataError> Initialize()
        {
            VoidWithDataError result = new VoidWithDataError();
            if (storage != null)
            {
                storage.CreateLinks();
                result = storage.CreateTable(PyramidInfo);
                return Task.FromResult(result);
            }
            result.Errors.Add(new DataError(DataErrorCode.StorageNotFound, "You must define a storage inside your DM " + GetType().Name));
            return Task.FromResult(result);
        }


        #endregion

        #region Get
        public override IQueryBuilder<X> CreateQuery<X>()
        {
            return new DatabaseQueryBuilder<X>(Storage) { UseShortObject = false };
        }

        private readonly Dictionary<Type, object> savedGetAllQuery = new();
        protected override ResultWithDataError<List<X>> GetAllLogic<X>()
        {
            if (NeedLocalCache)
            {
                return GetAllWithErrorCache<X>();
            }
            return GetAllWithErrorNoCache<X>();
        }
        protected ResultWithDataError<List<X>> GetAllWithErrorCache<X>() where X : U
        {
            Type type = typeof(X);
            Type rootType = typeof(U);
            if (GetAllDone.Contains(rootType) || GetAllDone.Contains(type))
            {
                ResultWithDataError<List<X>> result = new()
                {
                    Result = new List<X>()
                };
                foreach (KeyValuePair<int, U> record in Records)
                {
                    if (record is X casted)
                    {
                        result.Result.Add(casted);
                    }
                }
                return result;
            }
            ResultWithDataError<List<X>> resultNoCache = GetAllWithErrorNoCache<X>();
            if (resultNoCache.Success && resultNoCache.Result != null)
            {
                List<X> finalResult = new List<X>();
                foreach (X newRecord in resultNoCache.Result)
                {
                    if (!Records.ContainsKey(newRecord.Id))
                    {
                        finalResult.Add(newRecord);
                        Records.Add(newRecord.Id, newRecord);
                    }
                    else
                    {
                        U item = Records[newRecord.Id];
                        if (item is X casted)
                        {
                            finalResult.Add(casted);
                        }
                    }
                }
                resultNoCache.Result = finalResult;
                GetAllDone.Add(type);
            }
            return resultNoCache;
        }
        protected ResultWithDataError<List<X>> GetAllWithErrorNoCache<X>() where X : U
        {
            Type x = typeof(X);
            if (!savedGetAllQuery.ContainsKey(x))
            {
                savedGetAllQuery[x] = new DatabaseQueryBuilder<X>(Storage);
            }
            return ((DatabaseQueryBuilder<X>)savedGetAllQuery[x]).RunWithError();
        }


        private readonly Dictionary<Type, object> savedGetByIdQuery = new();
        protected override ResultWithDataError<X> GetByIdLogic<X>(int id)
        {
            if (NeedLocalCache)
            {
                return GetByIdWithErrorCache<X>(id);
            }
            return GetByIdWithErrorNoCache<X>(id);
        }

        public ResultWithDataError<X> GetByIdWithErrorCache<X>(int id) where X : U
        {
            if (Records.ContainsKey(id) && Records[id] is X casted)
            {
                ResultWithDataError<X> result = new()
                {
                    Result = casted
                };
                return result;
            }
            ResultWithDataError<X> resultNoCache = GetByIdWithErrorNoCache<X>(id);
            if (resultNoCache.Success && resultNoCache.Result != null)
            {
                Records[resultNoCache.Result.Id] = resultNoCache.Result;
            }
            return resultNoCache;
        }
        public ResultWithDataError<X> GetByIdWithErrorNoCache<X>(int id) where X : U
        {
            ResultWithDataError<X> result = new();

            Type x = typeof(X);
            if (!savedGetByIdQuery.ContainsKey(x))
            {
                DatabaseQueryBuilder<X> queryBuilderTemp = new(Storage);
                queryBuilderTemp.WhereWithParameters(i => i.Id == id);
                savedGetByIdQuery[x] = queryBuilderTemp;
            }

            DatabaseQueryBuilder<X> queryBuilder = (DatabaseQueryBuilder<X>)savedGetByIdQuery[x];
            queryBuilder.SetVariable(Storable.Id, id);
            ResultWithDataError<List<X>> resultTemp = queryBuilder.RunWithError();

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

        private readonly Dictionary<Type, object> savedGetByIdsQuery = new();
        protected override ResultWithDataError<List<X>> GetByIdsLogic<X>(List<int> ids)
        {
            if (NeedLocalCache)
            {
                return GetByIdsWithErrorCache<X>(ids);
            }
            return GetByIdsWithErrorNoCache<X>(ids);
        }

        public ResultWithDataError<List<X>> GetByIdsWithErrorCache<X>(List<int> ids) where X : U
        {
            ResultWithDataError<List<X>> result = new()
            {
                Result = new List<X>()
            };
            List<int> missingIds = new();
            foreach (int id in ids)
            {
                if (Records.ContainsKey(id))
                {
                    if (Records[id] is X casted)
                    {
                        result.Result.Add(casted);
                    }
                }
                else
                {
                    missingIds.Add(id);
                }
            }
            if (missingIds.Count > 0)
            {
                ResultWithDataError<List<X>> resultNoCache = GetByIdsWithErrorNoCache<X>(missingIds);
                if (resultNoCache.Success && resultNoCache.Result != null)
                {
                    foreach (X item in resultNoCache.Result)
                    {
                        if (!Records.ContainsKey(item.Id))
                        {
                            Records.Add(item.Id, item);
                        }
                        result.Result.Add(item);
                    }
                }
                else
                {
                    result.Result.Clear();
                    result.Errors.AddRange(resultNoCache.Errors);
                }
            }
            return result;
        }
        public ResultWithDataError<List<X>> GetByIdsWithErrorNoCache<X>(List<int> ids) where X : U
        {
            Type x = typeof(X);
            if (!savedGetByIdsQuery.ContainsKey(x))
            {
                DatabaseQueryBuilder<X> queryBuilderTemp = new(Storage);
                queryBuilderTemp.WhereWithParameters(i => ids.Contains(i.Id));
                savedGetByIdsQuery[x] = queryBuilderTemp;
            }

            DatabaseQueryBuilder<X> queryBuilder = (DatabaseQueryBuilder<X>)savedGetByIdsQuery[x];
            queryBuilder.SetVariable("ids", ids);
            ResultWithDataError<List<X>> resultTemp = queryBuilder.RunWithError();

            return resultTemp;
        }


        protected override ResultWithDataError<List<X>> WhereLogic<X>(Expression<Func<X, bool>> func)
        {
            if (NeedLocalCache)
            {
                return WhereWithErrorCache(func);
            }
            return WhereWithErrorNoCache(func);
        }

        public ResultWithDataError<List<X>> WhereWithErrorCache<X>(Expression<Func<X, bool>> func) where X : U
        {
            Type type = typeof(X);
            Type rootType = typeof(U);
            if (GetAllDone.Contains(rootType) || GetAllDone.Contains(type))
            {
                ResultWithDataError<List<X>> result = new()
                {
                    Result = new List<X>()
                };
                Func<X, bool> fct = func.Compile();
                foreach (KeyValuePair<int, U> pair in Records)
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

        public ResultWithDataError<List<X>> WhereWithErrorNoCache<X>(Expression<Func<X, bool>> func) where X : U
        {
            DatabaseQueryBuilder<X> queryBuilder = new(Storage);
            queryBuilder.Where(func);
            return queryBuilder.RunWithError();
        }

        #endregion

        #region Exist
        public override IExistBuilder<X> CreateExist<X>()
        {
            return new DatabaseExistBuilder<X>(Storage);
        }

        #endregion

        #region Create

        private readonly Dictionary<Type, object> savedCreateQuery = new();
        protected override ResultWithDataError<List<X>> CreateLogic<X>(List<X> values)
        {
            return Storage.RunInsideTransaction(new List<X>(), delegate ()
            {
                ResultWithDataError<List<X>> result = new()
                {
                    Result = new List<X>()
                };

                foreach (X value in values)
                {
                    Type type = value.GetType();
                    if (!savedCreateQuery.ContainsKey(type))
                    {
                        DatabaseCreateBuilder<X> query = new(Storage, type);
                        savedCreateQuery[type] = query;
                    }

                    ResultWithDataError<X> resultTemp = ((DatabaseCreateBuilder<X>)savedCreateQuery[type]).RunWithError(value);
                    if (resultTemp.Success && resultTemp.Result != null)
                    {
                        if (NeedLocalCache)
                        {
                            Records[resultTemp.Result.Id] = resultTemp.Result;
                        }
                        result.Result.Add(resultTemp.Result);
                    }
                    else
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                    }
                }

                return result;
            });
        }
        #endregion

        #region Update
        public override IUpdateBuilder<X> CreateUpdate<X>()
        {
            return new DatabaseUpdateBuilder<X>(Storage, this, NeedLocalCache);
        }
        private readonly Dictionary<Type, object> savedUpdateQuery = new();
        protected override ResultWithDataError<List<X>> UpdateLogic<X>(List<X> values)
        {
            return Storage.RunInsideTransaction(new List<X>(), delegate ()
            {
                ResultWithDataError<List<X>> result = new()
                {
                    Result = new List<X>()
                };
                int id = 0;
                foreach (X value in values)
                {
                    Type type = value.GetType();
                    if (!savedUpdateQuery.ContainsKey(type))
                    {
                        DatabaseUpdateBuilder<X> query = new(Storage, this, NeedLocalCache, type);
                        query.WhereWithParameters(p => p.Id == id);
                        savedUpdateQuery[type] = query;
                    }

                    ResultWithDataError<X> resultTemp = ((DatabaseUpdateBuilder<X>)savedUpdateQuery[type]).Prepare(value.Id).RunWithErrorSingle(value);
                    if (resultTemp.Success && resultTemp.Result != null)
                    {
                        result.Result.Add(resultTemp.Result);
                    }
                    else
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                    }
                }

                return result;
            });
        }
        #endregion

        #region Delete
        public override IDeleteBuilder<X> CreateDelete<X>()
        {
            return new DatabaseDeleteBuilder<X>(Storage, this, NeedLocalCache);
        }
        private readonly Dictionary<Type, object> savedDeleteQuery = new();
        protected override ResultWithDataError<List<X>> DeleteLogic<X>(List<X> values)
        {
            return Storage.RunInsideTransaction(new List<X>(), delegate ()
            {
                ResultWithDataError<List<X>> result = new()
                {
                    Result = new List<X>()
                };
                int id = 0;
                foreach (X value in values)
                {
                    Type type = value.GetType();
                    if (!savedDeleteQuery.ContainsKey(type))
                    {
                        DatabaseDeleteBuilder<X> query = new(Storage, this, NeedLocalCache, type);
                        query.WhereWithParameters(p => p.Id == id);
                        savedDeleteQuery[type] = query;
                    }

                    ResultWithDataError<List<X>> resultTemp = ((DatabaseDeleteBuilder<X>)savedDeleteQuery[type]).Prepare(value.Id).RunWithError();
                    if (resultTemp.Success && resultTemp.Result?.Count > 0)
                    {
                        result.Result.Add(resultTemp.Result[0]);
                    }
                    else
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                    }
                }

                return result;
            });
        }

        public List<X> RemoveRecordsItems<X>(List<int> ids) where X : IStorable
        {
            List<X> result = new();
            foreach (int id in ids)
            {
                if (Records.ContainsKey(id) && Records[id] is X casted)
                {
                    result.Add(casted);
                    Records.Remove(id);
                }
            }
            return result;
        }
        public List<X> RemoveRecordsItems<X>(List<X> items) where X : IStorable
        {
            List<X> result = new();
            foreach (X item in items)
            {
                if (item is U && Records.ContainsKey(item.Id) && Records[item.Id] is X casted)
                {
                    result.Add(casted);
                    Records.Remove(item.Id);
                }
            }
            return result;
        }

       
        #endregion

    }
}
