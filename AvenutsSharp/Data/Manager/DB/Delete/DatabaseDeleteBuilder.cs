using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Delete
{
    public class DatabaseDeleteBuilder<T> : DatabaseGenericBuilder<T>, ILambdaTranslatable, IDeleteBuilder<T> where T : IStorable
    {
        private readonly IGenericDM DM;
        private readonly bool NeedDeleteField;

        private Expression<Func<T, bool>>? whereFunc;
        private readonly Dictionary<string, object?> parameters = new();

        public DatabaseDeleteBuilder(IDBStorage storage, IGenericDM dm, bool needDeleteField, Type? baseType = null) : base(storage, baseType)
        {
            DM = dm;
            NeedDeleteField = needDeleteField;
            
        }


        public List<T>? Run()
        {
            ResultWithDataError<List<T>> result = RunWithError();
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return null;
        }

        public ResultWithDataError<List<T>> RunWithError()
        {
            ResultWithDataError<List<T>> result = new();
            if (whereFunc != null)
            {
                result = DM.WhereWithError<T>(whereFunc);
            }
            else
            {
                result = DM.GetAllWithError<T>();
            }

            if (result.Success && result.Result != null)
            {
                VoidWithDataError resultTemp = Storage.DeleteFromBuilder(this);
                if (resultTemp.Success && DM is IDatabaseDM databaseDM)
                {
                    if (NeedDeleteField)
                    {
                        result.Result = databaseDM.RemoveRecordsItems(result.Result);
                    }
                }
                else
                {
                    result.Errors.AddRange(resultTemp.Errors);
                }
            }
            else
            {
                result.Errors.AddRange(result.Errors);
            }

            return result;
        }

        public IDeleteBuilder<T> Prepare(params object[] objects)
        {
            PrepareGeneric(objects);
            return this;
        }

        public IDeleteBuilder<T> SetVariable(string name, object value)
        {
            SetVariableGeneric(name, value);
            return this;
        }

        protected override void OnVariableSet(ParamsInfo param, object fromObject)
        {
            string name = param.Name.Split(".").First();
            if (parameters.ContainsKey(name))
            {
                parameters[name] = fromObject;
            }
        }

        public IDeleteBuilder<T> Where(Expression<Func<T, bool>> func)
        {
            WhereGeneric(func);
            whereFunc = func;
            return this;
        }

        public IDeleteBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func)
        {
            
            WhereGenericWithParameters(func);
            Expression temp = new LambdaExtractVariables(parameters).Extract(func);
            if (temp is Expression<Func<T, bool>> casted)
            {
                whereFunc = casted;
            }
            else
            {
                throw new Exception("error when converting where with parameters function");
            }
            return this;
        }
    }
}
