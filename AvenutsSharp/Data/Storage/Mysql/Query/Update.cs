using AventusSharp.Data.Storage.Default;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Query
{
    internal class UpdateQueryInfo
    {
        public string sql { get; set; }
        public List<DbParameter> parameters { get; set; }
        public Func<IList, List<Dictionary<string, object>>> getParams { get; set; }
    }
    internal class Update
    {
        private static Dictionary<TableInfo, UpdateQueryInfo> queriesInfo = new Dictionary<TableInfo, UpdateQueryInfo>();

        private class UpdateQueryInfoTemp
        {
            public List<string> fields = new List<string>();
            public List<string> conditions = new List<string>();
            public List<DbParameter> parametersSQL = new List<DbParameter>();
            public Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();
            public TableMemberInfo updatedDate = null;
        }

        public static UpdateQueryInfo GetQueryInfo(TableInfo tableInfo, MySQLStorage storage)
        {
            if (!queriesInfo.ContainsKey(tableInfo))
            {
                createQueryInfo(tableInfo, storage);
            }
            return queriesInfo[tableInfo];
        }

        private static void loadMembers(TableInfo table, UpdateQueryInfoTemp info, MySQLStorage storage)
        {
            List<TableMemberInfoLink> allowedInfo = new List<TableMemberInfoLink>() { TableMemberInfoLink.None, TableMemberInfoLink.Simple, TableMemberInfoLink.Parent };
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsPrimary)
                {
                    string paramName = "@" + member.SqlName;
                    info.conditions.Add(member.SqlName + "=" + paramName);
                    info.memberByParameters.Add(paramName, member);

                    DbParameter parameter = storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    info.parametersSQL.Add(parameter);
                }
                else if (allowedInfo.Contains(member.link))
                {
                    string paramName = "@" + member.SqlName;
                    if (member.SqlName == "updatedDate")
                    {
                        info.fields.Add(member.SqlName + "=" + paramName);
                        info.updatedDate = member;
                    }
                    else if(member.SqlName != "createdDate")
                    {
                        info.fields.Add(member.SqlName + "=" + paramName);
                        info.memberByParameters.Add(paramName, member);
                    }

                    DbParameter parameter = storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    info.parametersSQL.Add(parameter);
                }
            }
        }


        private static void createQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "UPDATE " + table.SqlTableName + " SET $fields WHERE $condition";

            UpdateQueryInfoTemp infoTemp = new UpdateQueryInfoTemp();

            loadMembers(table, infoTemp, storage);

            if (infoTemp.fields.Count == 0)
            {
                UpdateQueryInfo infoFinal = new UpdateQueryInfo()
                {
                    sql = "",
                    getParams = null,
                    parameters = null,
                };

                queriesInfo[table] = infoFinal;
            }
            else
            {
                sql = sql.Replace("$fields", string.Join(",", infoTemp.fields));
                sql = sql.Replace("$condition", string.Join(" AND ", infoTemp.conditions));

                Func<IList, List<Dictionary<string, object>>> func = delegate (IList data)
                {
                    List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                    foreach (object item in data)
                    {
                        Dictionary<string, object> line = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, TableMemberInfo> param in infoTemp.memberByParameters)
                        {
                            line.Add(param.Key, param.Value.GetSqlValue(item));
                        }
                        if (infoTemp.updatedDate != null)
                        {
                            line.Add("@updatedDate", DateTime.Now);
                        }
                        result.Add(line);
                    }

                    return result;
                };

                Console.WriteLine(sql);

                UpdateQueryInfo infoFinal = new UpdateQueryInfo()
                {
                    sql = sql,
                    getParams = func,
                    parameters = infoTemp.parametersSQL,
                };

                queriesInfo[table] = infoFinal;
            }
        }

    }
}
