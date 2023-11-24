using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.TableMember;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Builders
{
    public class DatabaseUpdateBuilderInfo
    {
        public string UpdateSql { get; set; }
        public string QuerySql { get; set; }

        public List<TableReverseMemberInfo> ReverseMembers { get; set; } = new();

        public List<TableMemberInfoSql> ToCheckBefore { get; set; } = new();

        public DatabaseUpdateBuilderInfo(string updateSql, string querySql)
        {
            UpdateSql = updateSql;
            QuerySql = querySql;
        }
    }
    public class DatabaseUpdateBuilder<T> : DatabaseGenericBuilder<T>, ILambdaTranslatable, IUpdateBuilder<T> where T : IStorable
    {
        public Dictionary<string, ParamsInfo> UpdateParamsInfo { get; set; } = new Dictionary<string, ParamsInfo>();

        public DatabaseUpdateBuilderInfo? Query { get; set; }
        private readonly IGenericDM DM;
        private readonly bool NeedUpdateField;
        public bool AllFieldsUpdate { get; private set; } = true;

        public DatabaseUpdateBuilder(IDBStorage storage, IGenericDM dm, bool needUpdateField, Type? baseType = null) : base(storage, baseType)
        {
            DM = dm;
            NeedUpdateField = needUpdateField;
        }


        public List<T>? Run(T item)
        {
            ResultWithDataError<List<T>> result = RunWithError(item);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return null;
        }

        public ResultWithDataError<List<T>> RunWithError(T item)
        {
            ResultWithDataError<List<T>> result = new();
            ResultWithDataError<List<int>> resultTemp = Storage.UpdateFromBuilder(this, item);
            if (resultTemp.Success && resultTemp.Result != null)
            {
                ResultWithDataError<List<T>> resultQuery = DM.GetByIdsWithError<T>(resultTemp.Result);
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

        public ResultWithDataError<T> RunWithErrorSingle(T item)
        {
            ResultWithDataError<T> result = new();
            ResultWithDataError<List<T>> resultTemp = RunWithError(item);

            if (resultTemp.Success && resultTemp.Result != null)
            {
                if (resultTemp.Result.Count == 1)
                {
                    foreach (KeyValuePair<string, ParamsInfo> paramUpdated in UpdateParamsInfo)
                    {
                        paramUpdated.Value.SetCurrentValueOnObject(item);
                    }
                    result.Result = item;
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.NumberOfItemsNotMatching, "Can't update single because the action return " + resultTemp.Result.Count + " item"));
                }
            }
            else
                result.Errors.AddRange(resultTemp.Errors);
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

        public IUpdateBuilder<T> Field<U>(Expression<Func<T, U>> fct)
        {
            AllFieldsUpdate = false;
            string fieldPath = FieldGeneric(fct);
            string[] splitted = fieldPath.Split(".");
            string current = "";
            List<TableMemberInfoSql> access = new();
            string lastAlias = "";
            foreach (string s in splitted)
            {
                if (InfoByPath[current] != null)
                {
                    KeyValuePair<TableMemberInfoSql?, string> infoTemp = InfoByPath[current].GetTableMemberInfoAndAlias(s);
                    if (infoTemp.Key == null)
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

            TableMemberInfoSql lastMemberInfo = access.Last();
            if (lastMemberInfo is ITableMemberInfoSqlWritable writable)
            {
                string name = lastAlias + "." + lastMemberInfo.SqlName;
                UpdateParamsInfo.Add(name, new ParamsInfo()
                {
                    DbType = writable.SqlType,
                    Name = name,
                    MembersList = access,
                });
            }
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
