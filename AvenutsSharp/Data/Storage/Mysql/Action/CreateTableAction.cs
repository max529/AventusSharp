using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Data.Storage.Mysql.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class CreateTableAction : CreateTableAction<MySQLStorage>
    {
        public override void run(TableInfo table)
        {
            if (!Storage.TableExist(table))
            {
                string sql = CreateTable.GetQuery(Storage, table);
                Storage.Execute(sql);

                // create intermediate table
                List<TableMemberInfo> members = table.members.Where
                    (f => f.link ==  TableMemberInfoLink.Multiple).ToList();

                foreach (TableMemberInfo member in members)
                {
                    sql = CreateIntermediateTable.GetQuery(member);
                    Storage.Execute(sql);
                }
            }
            foreach (TableInfo child in table.Children)
            {
                run(child);
            }
        }
    }
}
