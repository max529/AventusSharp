using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB
{
    public class DatabaseDMSimple<T> : DatabaseDM<DatabaseDMSimple<T>, T> where T : IStorable { }
    public class DatabaseDM<T, U> : GenericDataManager<T, U> where T : IGenericDM<U>, new() where U : IStorable
    {
        protected IStorage storage;

        public override List<X> GetAll<X>()
        {
            return new List<X>();
        }

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


        public override ResultWithError<List<X>> CreateWithError<X>(List<X> values)
        {
            return storage.Create(values);
        }
    }
}
