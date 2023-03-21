using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
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
        private static Dictionary<TableInfo, string> queryByTable = new Dictionary<TableInfo, string>();
        private static Dictionary<TableInfo, List<DbParameter>> parametersByTable = new Dictionary<TableInfo, List<DbParameter>>();
        private static Dictionary<TableInfo, Func<IList, List<Dictionary<string, object>>>> paramsFctByTable = new Dictionary<TableInfo, Func<IList, List<Dictionary<string, object>>>>();
        public override List<X> run<X>(TableInfo table, List<X> data)
        {

            if (!queryByTable.ContainsKey(table))
            {
                this.createQuery(table);
            }

            DbCommand cmd = Storage.CreateCmd(queryByTable[table]);


            foreach (DbParameter parameter in parametersByTable[table])
            {
                cmd.Parameters.Add(parameter);
            }

            List<Dictionary<string, string>> allIds = Storage.Query(cmd, paramsFctByTable[table](data));
            if (allIds.Count == data.Count)
            {
                for (int i = 0; i < allIds.Count; i++)
                {
                    data[i].id = int.Parse(allIds[i]["id"]);
                }
            }



            throw new NotImplementedException();
        }

        private void createQuery(TableInfo table)
        {
            string sql = "INSERT INTO " + table.SqlTableName + " ($fields) VALUES ($values); SELECT last_insert_id() as id;";
            List<string> members = new List<string>();
            List<string> parameters = new List<string>();
            List<DbParameter> parametersSQL = new List<DbParameter>();
            Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();
            TableMemberInfo createdDate = null;
            TableMemberInfo updatedDate = null;
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsAutoIncrement)
                {
                    continue;
                }
                if (member.link == TableMemberInfoLink.None || member.link == TableMemberInfoLink.Simple)
                {
                    string paramName = "@" + member.SqlName;
                    members.Add("" + member.SqlName + "");
                    parameters.Add(paramName);
                    if (member.SqlName == "createdDate")
                    {
                        createdDate = member;
                    }
                    else if (member.SqlName == "updatedDate")
                    {
                        updatedDate = member;
                    }
                    else
                    {
                        memberByParameters.Add(paramName, member);
                    }

                    DbParameter parameter = Storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    parametersSQL.Add(parameter);
                }
            }
            sql = sql.Replace("$fields", string.Join(",", members));
            sql = sql.Replace("$values", string.Join(",", parameters));

            Func<IList, List<Dictionary<string, object>>> func = delegate (IList data)
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                foreach (object item in data)
                {
                    Dictionary<string, object> line = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, TableMemberInfo> param in memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }

                    DateTime now = DateTime.Now;
                    if (createdDate != null)
                    {
                        line.Add("@createdDate", now);
                    }
                    if (updatedDate != null)
                    {
                        line.Add("@updatedDate", now);
                    }
                    result.Add(line);
                }

                return result;
            };



            paramsFctByTable[table] = func;
            queryByTable[table] = sql;
            parametersByTable[table] = parametersSQL;
        }
    }
}
