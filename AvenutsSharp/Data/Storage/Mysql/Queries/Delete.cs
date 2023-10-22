using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Delete;
using AventusSharp.Data.Storage.Default;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace AventusSharp.Data.Storage.Mysql.Queries
{

    internal class Delete
    {
        private class DeleteQueryInfoTemp
        {
            public List<string> conditions = new();
            public List<DbParameter> parametersSQL = new();
            public Dictionary<string, TableMemberInfo> memberByParameters = new();

        }

        private static void LoadMembers(TableInfo table, DeleteQueryInfoTemp info, MySQLStorage storage)
        {
            foreach (TableMemberInfo member in table.Members)
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
        public static DeleteQueryInfo CreateQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "DELETE FROM " + table.SqlTableName + " WHERE $condition";

            DeleteQueryInfoTemp infoTemp = new();

            LoadMembers(table, infoTemp, storage);

            sql = sql.Replace("$condition", string.Join(" AND ", infoTemp.conditions));

            List<Dictionary<string, object?>> func(IList data)
            {
                List<Dictionary<string, object?>> result = new();
                foreach (object item in data)
                {
                    Dictionary<string, object?> line = new();
                    foreach (KeyValuePair<string, TableMemberInfo> param in infoTemp.memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }
                    result.Add(line);
                }

                return result;
            }

            DeleteQueryInfo infoFinal = new(
                sql: sql,
                getParams: func,
                parameters: infoTemp.parametersSQL
            );

            return infoFinal;
        }



        public static string PrepareSQL<X>(DatabaseDeleteBuilder<X> deleteBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseBuilderInfo mainInfo = deleteBuilder.InfoByPath[""];
            List<string> joins = new();
            List<string> aliases = new();

            void loadInfo(DatabaseBuilderInfo baseInfo)
            {
                string lastAlias = baseInfo.Alias;
                TableInfo lastTableInfo = baseInfo.TableInfo;
                aliases.Insert(0, lastAlias + ".*");
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.Parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;

                    joins.Add("INNER JOIN `" + info.SqlTableName + "` " + alias + " ON " + lastAlias + "." + lastTableInfo.Primary?.SqlName + "=" + alias + "." + info.Primary?.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                    aliases.Insert(0, lastAlias + ".*");
                }
            }
            loadInfo(mainInfo);

            string whereTxt = BuilderTools.Where(deleteBuilder.Wheres);
            
            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            string sql = "DELETE " + string.Join(",", aliases) + " FROM `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias
                + joinTxt
                + whereTxt;


            return sql;
        }

    }
}
