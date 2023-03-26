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
        protected IStorage storage;

        #region Config
        protected virtual IStorage DefineStorage()
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
            storage.CreateLinks();
            VoidWithError result = storage.CreateTable(pyramidInfo);
            if (result.Success)
            {
                return Task.FromResult(true);
            }

            foreach(DataError error in result.Errors)
            {
                error.Print();
            }
            return Task.FromResult(false);
        }
        #endregion

        #region Get
        public override ResultWithError<List<X>> GetAllWithError<X>()
        {
            return storage.GetAll<X>();
        }

        public override ResultWithError<X> GetByIdWithError<X>(int id)
        {
            return storage.GetById<X>(id);
        }

        public override ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func)
        {
            return storage.Where(func);
        }
        #endregion

        #region Create
        public override ResultWithError<List<X>> CreateWithError<X>(List<X> values)
        {
            return storage.Create(values);
        }
        #endregion

        #region Update
        public override ResultWithError<List<X>> UpdateWithError<X>(List<X> values)
        {
            return storage.Update(values);
        }
        #endregion

        #region Delete
        public override ResultWithError<List<X>> DeleteWithError<X>(List<X> values)
        {
            return storage.Delete(values);
        }
        #endregion

    }
}
