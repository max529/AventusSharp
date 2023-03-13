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
            Console.WriteLine("inside");
            return new List<X>();
        }

        protected virtual IStorage DefineStorage()
        {
            return null;
        }
        public override async Task<bool> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            this.storage = this.DefineStorage();
            if (storage == null)
            {
                storage = config.defaultStorage;
            }
            if(storage == null)
            {
                return false;
            }
            if (!storage.IsConnectedOneTime)
            {
                if(!storage.Connect())
                {
                    return false;
                }
            }
            storage.AddPyramid(pyramid);
            return await base.SetConfiguration(pyramid, config);
            
        }
        protected override async Task<bool> Initialize()
        {
            storage.CreateLinks();
            storage.CreateTable(pyramidInfo);
            return true;
        }
    }
}
