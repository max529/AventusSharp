using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class CreateAction : CreateAction<MySQLStorage>
    {
        private static Dictionary<TableInfo, string> queryByTable = new Dictionary<TableInfo, string>();
        private static Dictionary<TableInfo, Func<object, string>> valuesByTable = new Dictionary<TableInfo, Func<object, string>>();
        public override List<X> run<X>(TableInfo table, List<X> data)
        {
            if (!queryByTable.ContainsKey(table))
            {

            }
            throw new NotImplementedException();
        }

        private void createQuery(TableInfo table)
        {
            string sql = "INSERT INTO [" + table.SqlTableName + "] ($fields) VALUES ($values); SELECT SCOPE_IDENTITY();";
            List<string> members = new List<string>();
            foreach (TableMemberInfo member in table.members)
            {
                if (member.IsAutoIncrement)
                {
                    continue;
                }
                if(member.link == TableMemberInfoLink.None)
                {
                    members.Add("[" + member.SqlName + "]");
                }
                else if(member.link == TableMemberInfoLink.Simple)
                {
                    members.Add("[" + member.SqlName + "]");
                }
            }
            sql = sql.Replace("$fields", string.Join(",", members));
        }
    }
}
