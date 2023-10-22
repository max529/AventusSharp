using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Exist
{
    public class DatabaseExistBuilder<T> : DatabaseGenericBuilder<T>, IExistBuilder<T> where T : IStorable
    {

        public DatabaseExistBuilder(IDBStorage storage, Type? baseType = null) : base(storage, baseType)
        {
            
        }

        public IExistBuilder<T> Where(Expression<Func<T, bool>> func)
        {
            WhereGeneric(func);
            return this;
        }

        public IExistBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func)
        {
            WhereGenericWithParameters(func);
            return this;
        }
        public IExistBuilder<T> Prepare(params object[] objects)
        {
            PrepareGeneric(objects);
            return this;
        }
        public IExistBuilder<T> SetVariable(string name, object value)
        {
            SetVariableGeneric(name, value);
            return this;
        }
        public bool Run()
        {
            ResultWithDataError<bool> result = Storage.ExistFromBuilder(this);
            if (result.Success)
            {
                return result.Result;
            }
            return false;
        }

        public ResultWithDataError<bool> RunWithError()
        {
            return Storage.ExistFromBuilder(this);
        }

    }
}
