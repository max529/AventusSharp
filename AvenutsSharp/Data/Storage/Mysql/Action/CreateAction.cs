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
        private static Dictionary<TableInfo, Func<IList, List<Dictionary<string, string>>>> paramsByTable = new Dictionary<TableInfo, Func<IList, List<Dictionary<string, string>>>>();
        public override List<X> run<X>(TableInfo table, List<X> data)
        {
            if (!queryByTable.ContainsKey(table))
            {
                this.createQuery(table);
            }

            using (DbCommand cmd = Storage.CreateCmd(queryByTable[table]))
            {

                List<Dictionary<string, string>> allIds = Storage.Query(cmd, paramsByTable[table](data));
                if(allIds.Count == data.Count)
                {
                    for(int i = 0; i < allIds.Count; i++)
                    {
                        data[i].id = int.Parse(allIds[i]["id"]);
                    }
                }
            }


            throw new NotImplementedException();
        }

        private void createQuery(TableInfo table)
        {
            string sql = "INSERT INTO [" + table.SqlTableName + "] ($fields) VALUES ($values); SELECT last_insert_id();";
            List<string> members = new List<string>();
            List<string> parameters = new List<string>();
            Dictionary<string, TableMemberInfo> memberByParameters = new Dictionary<string, TableMemberInfo>();
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsAutoIncrement)
                {
                    continue;
                }
                if (member.link == TableMemberInfoLink.None)
                {
                    members.Add("[" + member.SqlName + "]");
                    parameters.Add("@" + member.SqlName);
                    memberByParameters.Add("@" + member.SqlName, member);
                }
                else if (member.link == TableMemberInfoLink.Simple)
                {
                    members.Add("[" + member.SqlName + "]");
                    parameters.Add("@" + member.SqlName);
                    memberByParameters.Add("@" + member.SqlName, member);
                }
            }
            sql = sql.Replace("$fields", string.Join(",", members));
            sql = sql.Replace("$values", string.Join(",", parameters));

            Func<IList, List<Dictionary<string, string>>> func = delegate (IList data)
            {
                List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
                foreach (object item in data)
                {
                    Dictionary<string, string> line = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, TableMemberInfo> param in memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }
                    result.Add(line);
                }
                return result;
            };



            paramsByTable[table] = func;
            queryByTable[table] = sql;
        }
    }
}
