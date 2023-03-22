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
            StorageQueryResult queryResult = Storage.Query("SELECT COUNT(*) nb FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + table.SqlTableName + "' and TABLE_SCHEMA = '" + this.Storage.GetDatabaseName() + "'; ");
            result.Errors.AddRange(queryResult.Errors);

            if (queryResult.Success && queryResult.Result.Count == 1)
            {
                int nb = int.Parse(queryResult.Result.ElementAt(0)["nb"]);
                if (nb == 0)
                {
                    result.Result = false;
                }
                result.Result = true;
            }
            return result;
        }
    }
}
