using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Update
{
    public class DatabaseUpdateBuilder<T> : DatabaseGenericBuilder<T>, ILambdaTranslatable, IUpdateBuilder<T> where T : IStorable
    {
        public Dictionary<string, ParamsInfo> UpdateParamsInfo { get; set; } = new Dictionary<string, ParamsInfo>();

        public string SqlQuery { get; set; } = "";
        private readonly IGenericDM DM;
        private readonly bool NeedUpdateField;
        public bool AllFieldsUpdate { get; private set; } = true;

        public DatabaseUpdateBuilder(IDBStorage storage, IGenericDM dm, bool needUpdateField, Type? baseType = null) : base(storage, baseType)
        {
            this.DM = dm;
            this.NeedUpdateField = needUpdateField;
        }


        public List<T>? Run(T item)
        {
            ResultWithError<List<T>> result = RunWithError(item);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return null;
        }

        public ResultWithError<List<T>> RunWithError(T item)
        {
            ResultWithError<List<T>> result = new();
            ResultWithError<List<int>> resultTemp = Storage.UpdateFromBuilder(this, item);
            if (resultTemp.Success && resultTemp.Result != null)
            {
                ResultWithError<List<T>> resultQuery = DM.GetByIdsWithError<T>(resultTemp.Result);
                if (resultQuery.Success && resultQuery.Result != null)
                {
                    // update data in cache
                    if (NeedUpdateField)
                    {
                        foreach (KeyValuePair<string, ParamsInfo> paramUpdated in UpdateParamsInfo)
                        {
                            foreach (T resultItem in resultQuery.Result)
                            {
                                paramUpdated.Value.SetCurrentValueOnObject(resultItem);
                            }
                        }
                    }
                    result.Result = resultQuery.Result;
                }
                else
                {
                    result.Errors.AddRange(resultTemp.Errors);
                }
            }
            else
            {
                result.Errors.AddRange(resultTemp.Errors);
            }

            return result;
        }

        public IUpdateBuilder<T> Prepare(params object[] objects)
        {
            PrepareGeneric(objects);
            return this;
        }

        public IUpdateBuilder<T> SetVariable(string name, object value)
        {
            SetVariableGeneric(name, value);
            return this;
        }

        public IUpdateBuilder<T> Field(Expression<Func<T, object>> fct)
        {
            AllFieldsUpdate = false;
            string fieldPath = FieldGeneric(fct);
            string[] splitted = fieldPath.Split(".");
            string current = "";
            List<TableMemberInfo> access = new();
            string lastAlias = "";
            foreach (string s in splitted)
            {
                if (InfoByPath[current] != null)
                {
                    KeyValuePair<TableMemberInfo?, string> infoTemp = InfoByPath[current].GetTableMemberInfoAndAlias(s);
                    if(infoTemp.Key == null)
                    {
                        throw new Exception("Can't find the field " + s + " on the path " + current);
                    }
                    access.Add(infoTemp.Key);
                    lastAlias = infoTemp.Value;
                    if (current != "")
                    {
                        current += "." + s;
                    }
                    else
                    {
                        current += s;
                    }
                }
            }

            TableMemberInfo lastMemberInfo = access.Last();
            string name = lastAlias + "." + lastMemberInfo.SqlName;
            UpdateParamsInfo.Add(name, new ParamsInfo()
            {
                DbType = lastMemberInfo.SqlType,
                Name = name,
                MembersList = access,
            });
            return this;
        }

        public IUpdateBuilder<T> Where(Expression<Func<T, bool>> func)
        {
            WhereGeneric(func);
            return this;
        }

        public IUpdateBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func)
        {
            WhereGenericWithParameters(func);
            return this;
        }
    }
}
