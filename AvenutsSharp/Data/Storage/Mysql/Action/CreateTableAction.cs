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
        public override VoidWithError run(TableInfo table)
        {
            VoidWithError result = new VoidWithError();
            ResultWithError<bool> tableExist = Storage.TableExist(table);
            result.Errors.AddRange(tableExist.Errors);
            if (tableExist.Success && !tableExist.Result)
            {
                string sql = CreateTable.GetQuery(Storage, table);
                StorageExecutResult resultTemp = Storage.Execute(sql);
                result.Errors.AddRange(resultTemp.Errors);

                // create intermediate table
                List<TableMemberInfo> members = table.members.Where
                    (f => f.link == TableMemberInfoLink.Multiple).ToList();

                string? intermediateQuery = null;
                foreach (TableMemberInfo member in members)
                {
                    intermediateQuery = CreateIntermediateTable.GetQuery(member);
                    StorageExecutResult resultTempInter = Storage.Execute(sql);
                    result.Errors.AddRange(resultTempInter.Errors);
                }
            }
            foreach (TableInfo child in table.Children)
            {
                VoidWithError resultTemp = run(child);
                result.Errors.AddRange(resultTemp.Errors);
            }
            return result;
        }
    }
}
