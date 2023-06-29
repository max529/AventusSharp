using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB.Update
{
    public class DatabaseUpdateBuilder<T> : DatabaseGenericBuilder<T>, ILambdaTranslatable, UpdateBuilder<T>
    {
        public Dictionary<string, ParamsInfo> updateParamsInfo { get; set; } = new Dictionary<string, ParamsInfo>();

        public DatabaseUpdateBuilder(IStorage storage) : base(storage)
        {
        }


        public T? Run(T item)
        {
            ResultWithError<T> result = RunWithError(item);
            if (result.Success && result.Result != null)
            {
                return result.Result;
            }
            return default(T);
        }

        public ResultWithError<T> RunWithError(T item)
        {
            return Storage.UpdateFromBuilder(this, item);
        }

        public UpdateBuilder<T> Prepare(params object[] objects)
        {
            _Prepare(objects);
            return this;
        }

        public UpdateBuilder<T> SetVariable(string name, object value)
        {
            _SetVariable(name, value);
            return this;
        }

        public UpdateBuilder<T> UpdateField(Expression<Func<T, object>> fct)
        {
            string fieldPath = _Field(fct);
            string[] splitted = fieldPath.Split(".");
            string current = "";
            List<TableMemberInfo> access = new List<TableMemberInfo>();
            string lastAlias = "";
            foreach (string s in splitted)
            {
                if (infoByPath[current] != null)
                {
                    KeyValuePair<TableMemberInfo, string> infoTemp = infoByPath[current].GetTableMemberInfoAndAlias(s);
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
            updateParamsInfo.Add(name, new ParamsInfo()
            {
                dbType = lastMemberInfo.SqlType,
                name = name,
                membersList = access,
            });
            return this;
        }

        public UpdateBuilder<T> Where(Expression<Func<T, bool>> func)
        {
            _Where(func);
            return this;
        }

        public UpdateBuilder<T> WhereWithParameters(Expression<Func<T, bool>> func)
        {
            _WhereWithParameters(func);
            return this;
        }
    }
}
