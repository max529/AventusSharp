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
    internal class GetAllAction : GetAllAction<MySQLStorage>
    {
        private static Dictionary<TableInfo, string> queryByTable = new Dictionary<TableInfo, string>();
        private static Dictionary<TableInfo, List<DbParameter>> parametersByTable = new Dictionary<TableInfo, List<DbParameter>>();
        public override ResultWithError<List<X>> run<X>(TableInfo table)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Not implemented"));
            return result;

           // ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();

            if (!queryByTable.ContainsKey(table))
            {
                getAllQuery(table);
            }

            return result;
        }

        private void getAllQuery(TableInfo table)
        {
            string sql = "SELECT $fields FROM " + table.SqlTableName;

            foreach (TableMemberInfo member in table.members)
            {
            }
        }
    }
}
