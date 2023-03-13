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
        public override bool run(TableInfo table)
        {
            List<Dictionary<string, string>> res = Storage.Query("SELECT COUNT(*) res FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + table.SqlTableName + "' and TABLE_SCHEMA = '" + this.Storage.GetDatabaseName() + "'; ");
            if (res.Count == 1)
            {
                int nb = int.Parse(res.ElementAt(0)["res"]);
                if (nb == 0)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
