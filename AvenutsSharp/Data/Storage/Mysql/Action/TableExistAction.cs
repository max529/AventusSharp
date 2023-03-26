using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class TableExistAction : TableExistAction<MySQLStorage>
    {
        public override ResultWithError<bool> run(TableInfo table)
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
            string sql = "SELECT COUNT(*) nb FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + table.SqlTableName + "' and TABLE_SCHEMA = '" + this.Storage.GetDatabaseName() + "'; ";
            StorageQueryResult queryResult = Storage.Query(sql);
            result.Errors.AddRange(queryResult.Errors);

            if (queryResult.Success && queryResult.Result.Count == 1)
            {
                int nb = int.Parse(queryResult.Result.ElementAt(0)["nb"]);
                result.Result = (nb != 0);
            }
            return result;
        }
    }
}
