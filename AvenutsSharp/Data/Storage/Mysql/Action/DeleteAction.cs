using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Data.Storage.Mysql.Query;
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
        public override ResultWithError<List<X>> run<X>(TableInfo table, List<X> data)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();

            // group data by type
            ResultWithError<Dictionary<TableInfo, IList>> orderedData = Storage.GroupDataByType(table, data);
            result.Errors.AddRange(orderedData.Errors);
            if (!result.Success)
            {
                return result;
            }
            // delete group of data
            foreach (KeyValuePair<TableInfo, IList> pair in orderedData.Result)
            {
                ResultWithError<IList> resultTemp = DeleteByType(pair.Key, pair.Value);
                result.Errors.AddRange(resultTemp.Errors);
                if (!result.Success)
                {
                    return result;
                }
            }

            // No error
            result.Result = data;

            return result;
        }

        /// <summary>
        /// Insert data by type
        /// </summary>
        /// <param name="table"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private ResultWithError<IList> DeleteByType(TableInfo table, IList data)
        {
            ResultWithError<IList> result = new ResultWithError<IList>();

            DeleteQueryInfo info = Delete.GetQueryInfo(table, Storage);

            DbCommand cmd = Storage.CreateCmd(info.sql);
            foreach (DbParameter parameter in info.parameters)
            {
                cmd.Parameters.Add(parameter);
            }

            StorageExecutResult queryResult = Storage.Execute(cmd, info.getParams(data));
            cmd.Dispose();
            result.Errors.AddRange(queryResult.Errors);

            // load parent
            if (table.Parent != null)
            {
                ResultWithError<IList> resultTemp = DeleteByType(table.Parent, data);
                result.Errors.AddRange(resultTemp.Errors);
                if (!result.Success)
                {
                    return result;
                }
            }

            return result;
        }

    }
}
