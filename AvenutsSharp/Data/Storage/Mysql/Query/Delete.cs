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
    internal class DeleteQueryInfo
    {
        public string sql { get; set; }
        public List<DbParameter> parameters { get; set; }
        public Func<IList, List<Dictionary<string, object?>>> getParams { get; set; }

        public DeleteQueryInfo(string sql, List<DbParameter> parameters, Func<IList, List<Dictionary<string, object?>>> getParams)
        {
            this.sql = sql;
            this.parameters = parameters;
            this.getParams = getParams;
        }
    }
    internal class Delete
    {
        private static Dictionary<TableInfo, DeleteQueryInfo> queriesInfo = new Dictionary<TableInfo, DeleteQueryInfo>();

        private class DeleteQueryInfoTemp
        {
            public List<string> conditions = new List<string>();
            public List<DbParameter> parametersSQL = new List<DbParameter>();
            public Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();

        }

        public static DeleteQueryInfo GetQueryInfo(TableInfo tableInfo, MySQLStorage storage)
        {
            if (!queriesInfo.ContainsKey(tableInfo))
            {
                createQueryInfo(tableInfo, storage);
            }
            return queriesInfo[tableInfo];
        }

        private static void loadMembers(TableInfo table, DeleteQueryInfoTemp info, MySQLStorage storage)
        {
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
            }
        }


        private static void createQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "DELETE FROM " + table.SqlTableName + " WHERE $condition";

            DeleteQueryInfoTemp infoTemp = new DeleteQueryInfoTemp();

            loadMembers(table, infoTemp, storage);

            sql = sql.Replace("$condition", string.Join(" AND ", infoTemp.conditions));

            Func<IList, List<Dictionary<string, object?>>> func = delegate (IList data)
            {
                List<Dictionary<string, object?>> result = new List<Dictionary<string, object?>>();
                foreach (object item in data)
                {
                    Dictionary<string, object?> line = new Dictionary<string, object?>();
                    foreach (KeyValuePair<string, TableMemberInfo> param in infoTemp.memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }
                    result.Add(line);
                }

                return result;
            };

            Console.WriteLine(sql);

            DeleteQueryInfo infoFinal = new DeleteQueryInfo(
                sql: sql,
                getParams: func,
                parameters: infoTemp.parametersSQL
            );

            queriesInfo[table] = infoFinal;
        }

    }
}
