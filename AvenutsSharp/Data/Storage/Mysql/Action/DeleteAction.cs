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
    internal class DeleteAction : DeleteAction<MySQLStorage>
    {
        private static Dictionary<TableInfo, string> queryByTable = new Dictionary<TableInfo, string>();
        private static Dictionary<TableInfo, List<DbParameter>> parametersByTable = new Dictionary<TableInfo, List<DbParameter>>();
        private static Dictionary<TableInfo, Func<IList, List<Dictionary<string, object>>>> paramsFctByTable = new Dictionary<TableInfo, Func<IList, List<Dictionary<string, object>>>>();

        public override ResultWithError<List<X>> run<X>(TableInfo table, List<X> data)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Not implemented"));
            return result;


            //ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();

            if (!queryByTable.ContainsKey(table))
            {
                deleteQuery(table);
            }

            DbCommand cmd = Storage.CreateCmd(queryByTable[table]);
            foreach (DbParameter parameter in parametersByTable[table])
            {
                cmd.Parameters.Add(parameter);
            }
            StorageExecutResult queryResult = Storage.Execute(cmd, paramsFctByTable[table](data));
            cmd.Dispose();
            result.Errors.AddRange(queryResult.Errors);
            if (result.Success)
            {
                result.Result = data;
            }
            return result;
        }

        #region prepare query
        private class DeleteQueryInfo
        {
            public List<string> conditions = new List<string>();
            public List<DbParameter> parametersSQL = new List<DbParameter>();
            public Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();
        }

        private void loadMembers(TableInfo table, DeleteQueryInfo info)
        {
            if (table.Parent != null)
            {
                loadMembers(table.Parent, info);
            }
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsPrimary)
                {
                    string paramName = "@" + member.SqlName;
                    info.conditions.Add(member.SqlName + "=" + paramName);

                    info.memberByParameters.Add(paramName, member);

                    DbParameter parameter = Storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    info.parametersSQL.Add(parameter);
                }
            }

        }
        private void deleteQuery(TableInfo table)
        {
            string sql = "DELETE FROM " + table.SqlTableName + " WHERE $conditons";

            DeleteQueryInfo info = new DeleteQueryInfo();

            
            sql = sql.Replace("$conditons", string.Join(" AND ", info.conditions));

            Func<IList, List<Dictionary<string, object>>> func = delegate (IList data)
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                foreach (object item in data)
                {
                    Dictionary<string, object> line = new Dictionary<string, object>();
                    foreach (KeyValuePair<string, TableMemberInfo> param in info.memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }
                    result.Add(line);
                }

                return result;
            };

            paramsFctByTable[table] = func;
            queryByTable[table] = sql;
            parametersByTable[table] = info.parametersSQL;
        }

        #endregion
    }
}
