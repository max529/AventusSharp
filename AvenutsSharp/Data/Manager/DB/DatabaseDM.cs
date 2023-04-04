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
        protected IStorage? storage;

        protected IStorage Storage
        {
            get
            {
                if(storage != null)
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
        public override ResultWithError<List<X>> GetAllWithError<X>()
        {
            return Storage.GetAll<X>();
        }

        public override ResultWithError<X> GetByIdWithError<X>(int id)
        {
            return Storage.GetById<X>(id);
        }

        public override ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func)
        {
            return Storage.Where(func);
        }
        #endregion

        #region Create
        public override ResultWithError<List<X>> CreateWithError<X>(List<X> values)
        {
            return Storage.Create(values);
        }
        #endregion

        #region Update
        public override ResultWithError<List<X>> UpdateWithError<X>(List<X> values)
        {
            return Storage.Update(values);
        }
        #endregion

        #region Delete
        public override ResultWithError<List<X>> DeleteWithError<X>(List<X> values)
        {
            return Storage.Delete(values);
        }
        #endregion

    }
}
