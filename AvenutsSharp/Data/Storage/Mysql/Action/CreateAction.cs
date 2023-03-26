using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class CreateAction : CreateAction<MySQLStorage>
    {
        private class CreateQueryInfo
        {
            public string sql { get; set; }
            public List<DbParameter> parameters { get; set; }
            public Func<IList, List<Dictionary<string, object>>> getParams { get; set; }

            public bool isRoot { get; set; }
        }
        private static Dictionary<TableInfo, CreateQueryInfo> createInfo = new Dictionary<TableInfo, CreateQueryInfo>();

        public override ResultWithError<List<X>> run<X>(TableInfo table, List<X> data)
        {
            OrderData(table, data);
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();

            if (table.Parent != null)
            {
                ResultWithError<List<X>> resultTemp = run(table.Parent, data);
                result.Errors.AddRange(resultTemp.Errors);
                if (!result.Success)
                {
                    return result;
                }
            }

            if (!createInfo.ContainsKey(table))
            {
                createQuery(table);
            }

            DbCommand cmd = Storage.CreateCmd(createInfo[table].sql);


            foreach (DbParameter parameter in createInfo[table].parameters)
            {
                cmd.Parameters.Add(parameter);
            }

            StorageQueryResult queryResult = Storage.Query(cmd, createInfo[table].getParams(data));
            cmd.Dispose();
            result.Errors.AddRange(queryResult.Errors);
            if (queryResult.Success && createInfo[table].isRoot)
            {
                if (queryResult.Result.Count == data.Count)
                {
                    for (int i = 0; i < queryResult.Result.Count; i++)
                    {
                        data[i].id = int.Parse(queryResult.Result[i]["id"]);
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Everythink seems to be ok but number of ids returned != nb element created"));
                }
            }

            return result;
        }

        private ResultWithError<Dictionary<TableInfo, IList>> OrderData(TableInfo table, IList data)
        {
            ResultWithError<Dictionary<TableInfo, IList>> result = new ResultWithError<Dictionary<TableInfo, IList>>();
            result.Result = new Dictionary<TableInfo, IList>();
            if (table.IsAbstract)
            {
                Dictionary<Type, TableInfo> loadedType = new Dictionary<Type, TableInfo>();
                foreach(object item in data)
                {
                    Type type = item.GetType();
                    if (!loadedType.ContainsKey(type))
                    {
                        TableInfo tableInfo = Storage.getTableInfo(type);
                        if(tableInfo == null)
                        {
                            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "this must be impossible"));
                            return result;
                        }
                        else
                        {
                            loadedType.Add(type, tableInfo);
                            Type newListType = typeof(List<>).MakeGenericType(type);
                            IList newList = (IList)TypeTools.CreateNewObj(newListType);
                            result.Result.Add(tableInfo, newList);
                        }
                    }
                    result.Result[loadedType[type]].Add(item);
                }
            }
            else
            {
                result.Result.Add(table, data);
            }
            return result;
        }

        #region prepare query

        private class CreateQueryInfoTemp
        {
            public List<string> members = new List<string>();
            public List<string> parameters = new List<string>();
            public List<DbParameter> parametersSQL = new List<DbParameter>();
            public Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();
            public TableMemberInfo createdDate = null;
            public TableMemberInfo updatedDate = null;

        }

        private void loadMembers(TableInfo table, CreateQueryInfoTemp createInfo)
        {
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsAutoIncrement)
                {
                    continue;
                }
                List<TableMemberInfoLink> allowedInfo = new List<TableMemberInfoLink>() { TableMemberInfoLink.None, TableMemberInfoLink.Simple, TableMemberInfoLink.Parent };
                if (allowedInfo.Contains(member.link))
                {
                    string paramName = "@" + member.SqlName;
                    createInfo.members.Add("" + member.SqlName + "");
                    createInfo.parameters.Add(paramName);
                    if (member.SqlName == "createdDate")
                    {
                        createInfo.createdDate = member;
                    }
                    else if (member.SqlName == "updatedDate")
                    {
                        createInfo.updatedDate = member;
                    }
                    else
                    {
                        createInfo.memberByParameters.Add(paramName, member);
                    }

                    DbParameter parameter = Storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    createInfo.parametersSQL.Add(parameter);
                }
            }
        }

        private void createQuery(TableInfo table)
        {
            string sql = "INSERT INTO " + table.SqlTableName + " ($fields) VALUES ($values); SELECT last_insert_id() as id;";

            CreateQueryInfoTemp createInfoTemp = new CreateQueryInfoTemp();

            loadMembers(table, createInfoTemp);

            sql = sql.Replace("$fields", string.Join(",", createInfoTemp.members));
            sql = sql.Replace("$values", string.Join(",", createInfoTemp.parameters));

            Func<IList, List<Dictionary<string, object>>> func = delegate (IList data)
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                foreach (object item in data)
                {
                    Dictionary<string, object> line = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, TableMemberInfo> param in createInfoTemp.memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }

                    DateTime now = DateTime.Now;
                    if (createInfoTemp.createdDate != null)
                    {
                        line.Add("@createdDate", now);
                    }
                    if (createInfoTemp.updatedDate != null)
                    {
                        line.Add("@updatedDate", now);
                    }
                    result.Add(line);
                }

                return result;
            };

            Console.WriteLine(sql);

            CreateQueryInfo infoFinal = new CreateQueryInfo()
            {
                sql = sql,
                getParams = func,
                parameters = createInfoTemp.parametersSQL,
                isRoot = (table.Parent == null)
            };

            createInfo[table] = infoFinal;
        }

        #endregion
    }
}
