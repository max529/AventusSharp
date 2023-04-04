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
    internal class CreateQueryInfo
    {
        public string sql { get; set; }
        public List<DbParameter> parameters { get; set; }
        public Func<IList, List<Dictionary<string, object?>>> getParams { get; set; }

        public bool isRoot { get; set; }

        public CreateQueryInfo(string sql, List<DbParameter> parameters, Func<IList, List<Dictionary<string, object?>>> getParams, bool isRoot)
        {
            this.sql = sql;
            this.parameters = parameters;
            this.getParams = getParams;
            this.isRoot = isRoot;
        }
    }
    internal class Create
    {
        private static Dictionary<TableInfo, CreateQueryInfo> createInfo = new Dictionary<TableInfo, CreateQueryInfo>();


        private class CreateQueryInfoTemp
        {
            public List<string> members = new List<string>();
            public List<string> parameters = new List<string>();
            public List<DbParameter> parametersSQL = new List<DbParameter>();
            public Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();
            public TableMemberInfo? createdDate = null;
            public TableMemberInfo? updatedDate = null;

        }

        public static CreateQueryInfo GetQueryInfo(TableInfo tableInfo, MySQLStorage storage)
        {
            if (!createInfo.ContainsKey(tableInfo))
            {
                createQueryInfo(tableInfo, storage);
            }
            return createInfo[tableInfo];
        }

        private static void loadMembers(TableInfo table, CreateQueryInfoTemp createInfo, MySQLStorage storage)
        {
            List<TableMemberInfoLink> allowedInfo = new List<TableMemberInfoLink>() { TableMemberInfoLink.None, TableMemberInfoLink.Simple, TableMemberInfoLink.Parent };
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsAutoIncrement)
                {
                    continue;
                }
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

                    DbParameter parameter = storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    createInfo.parametersSQL.Add(parameter);
                }
            }
        }

        private static void createQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "INSERT INTO " + table.SqlTableName + " ($fields) VALUES ($values);";
            if (table.Parent == null)
            {
                sql += " SELECT last_insert_id() as id;";
            }
            CreateQueryInfoTemp createInfoTemp = new CreateQueryInfoTemp();

            loadMembers(table, createInfoTemp, storage);

            sql = sql.Replace("$fields", string.Join(",", createInfoTemp.members));
            sql = sql.Replace("$values", string.Join(",", createInfoTemp.parameters));

            Func<IList, List<Dictionary<string, object?>>> func = delegate (IList data)
            {
                List<Dictionary<string, object?>> result = new List<Dictionary<string, object?>>();
                foreach (object item in data)
                {
                    Dictionary<string, object?> line = new Dictionary<string, object?>();
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

            CreateQueryInfo infoFinal = new CreateQueryInfo(
                sql: sql,
                getParams: func,
                parameters: createInfoTemp.parametersSQL,
                isRoot: (table.Parent == null)
            );

            createInfo[table] = infoFinal;
        }

    }
}
