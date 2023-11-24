﻿using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.TableMember;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace AventusSharp.Data.Manager.DB.Builders
{
    public class DatabaseDeleteBuilderInfo
    {
        public string Sql;
        public Dictionary<string, Dictionary<string, ParamsInfo>> DeleteNM = new Dictionary<string, Dictionary<string, ParamsInfo>>();

        public List<TableReverseMemberInfo> ReverseMembers = new List<TableReverseMemberInfo>();

        public DatabaseDeleteBuilderInfo(string sql)
        {
            Sql = sql;
        }
    }
    public class DatabaseDeleteBuilder<T> : DatabaseGenericBuilder<T>, ILambdaTranslatable, IDeleteBuilder<T> where T : IStorable
    {
        private readonly IGenericDM DM;
        private readonly bool NeedDeleteField;

        private readonly Dictionary<string, object?> parameters = new();

        public DatabaseDeleteBuilderInfo? info = null;

        public DatabaseQueryBuilder<T> queryBuilder;


        public DatabaseDeleteBuilder(IDBStorage storage, IGenericDM dm, bool needDeleteField, Type? baseType = null) : base(storage, baseType)
        {
            DM = dm;
            NeedDeleteField = needDeleteField;
            queryBuilder = new DatabaseQueryBuilder<T>(storage);
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
            ResultWithDataError<List<T>> result = queryBuilder.RunWithError();

            if (result.Success && result.Result != null)
            {
                VoidWithDataError resultTemp = Storage.DeleteFromBuilder(this, result.Result);
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
            queryBuilder.Prepare(objects);
            return this;
        }

        public IDeleteBuilder<T> SetVariable(string name, object value)
        {
            SetVariableGeneric(name, value);
            queryBuilder.SetVariable(name, value);
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
            queryBuilder.Where(func);
            return this;
        }

        public IDeleteBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func)
        {

            WhereGenericWithParameters(func);
            queryBuilder.WhereWithParameters(func);
            return this;
        }
    }
}
