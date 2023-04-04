using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Data.Storage.Mysql.Query;
using AventusSharp.Tools;
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
        public override ResultWithError<List<X>> run<X>(TableInfo table, List<X> data)
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
            // create group of data
            foreach (KeyValuePair<TableInfo, IList> pair in orderedData.Result)
            {
                ResultWithError<IList> resultTemp = CreateByType(pair.Key, pair.Value);
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
        private ResultWithError<IList> CreateByType(TableInfo table, IList data)
        {
            ResultWithError<IList> result = new ResultWithError<IList>();
            // load parent
            if (table.Parent != null)
            {
                ResultWithError<IList> resultTemp = CreateByType(table.Parent, data);
                result.Errors.AddRange(resultTemp.Errors);
                if (!result.Success)
                {
                    return result;
                }
            }

            CreateQueryInfo info = Create.GetQueryInfo(table, Storage);

            ResultWithError<DbCommand> cmdResult = Storage.CreateCmd(info.sql);
            result.Errors.AddRange(cmdResult.Errors);
            if(!result.Success || cmdResult.Result == null)
            {
                return result;
            }
            DbCommand cmd = cmdResult.Result;
            foreach (DbParameter parameter in info.parameters)
            {
                cmd.Parameters.Add(parameter);
            }

            StorageQueryResult queryResult = Storage.Query(cmd, info.getParams(data));
            cmd.Dispose();
            result.Errors.AddRange(queryResult.Errors);
            if (queryResult.Success && info.isRoot)
            {
                if (queryResult.Result.Count == data.Count)
                {
                    for (int i = 0; i < queryResult.Result.Count; i++)
                    {
                        if (data[i] is IStorable storable)
                        {
                            storable.id = int.Parse(queryResult.Result[i]["id"]);
                        }
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Everythink seems to be ok but number of ids returned != nb element created"));
                }
            }

            return result;
        }
    }
}
