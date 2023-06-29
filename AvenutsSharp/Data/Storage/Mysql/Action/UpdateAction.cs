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
    internal class UpdateAction : UpdateAction<MySQLStorage>
    {
        public override ResultWithError<List<X>> run<X>(TableInfo table, List<X> data, List<X>? oldData)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();

            // group data by type
            ResultWithError<Dictionary<TableInfo, IList>> orderedData = Storage.GroupDataByType(table, data);
            result.Errors.AddRange(orderedData.Errors);
            if (!result.Success || orderedData.Result == null)
            {
                return result;
            }
            
            // delete group of data
            foreach (KeyValuePair<TableInfo, IList> pair in orderedData.Result)
            {
                ResultWithError<IList> resultTemp = UpdateByType(pair.Key, pair.Value);
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
        private ResultWithError<IList> UpdateByType(TableInfo table, IList data)
        {
            ResultWithError<IList> result = new ResultWithError<IList>();

            UpdateQueryInfo info = Update.GetQueryInfo(table, Storage);
            // if sql == "" it means no fields can be updated inside this class
            if (info.sql != "")
            {
                List<DbParameter>? parameters = info.parameters;
                Func<IList, List<Dictionary<string, object?>>>? getParams = info.getParams;
                if (parameters != null && getParams != null)
                {
                    ResultWithError<DbCommand> cmdResult = Storage.CreateCmd(info.sql);
                    result.Errors.AddRange(cmdResult.Errors);
                    if (!result.Success || cmdResult.Result == null)
                    {
                        return result;
                    }
                    DbCommand cmd = cmdResult.Result;

                    foreach (DbParameter parameter in parameters)
                    {
                        cmd.Parameters.Add(parameter);
                    }

                    StorageExecutResult queryResult = Storage.Execute(cmd, getParams(data));
                    cmd.Dispose();
                    result.Errors.AddRange(queryResult.Errors);
                }
            }

            // load parent
            if (table.Parent != null)
            {
                ResultWithError<IList> resultTemp = UpdateByType(table.Parent, data);
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
