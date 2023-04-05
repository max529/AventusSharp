using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Data.Storage.Mysql.Query;
using AventusSharp.Data.Storage.Mysql.Tools;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class GetByIdAction : GetByIdAction<MySQLStorage>
    {
        public override ResultWithError<X> run<X>(TableInfo table, int id)
        {
            ResultWithError<X> result = new ResultWithError<X>();
            
            GetAllQueryInfo info = GetAll.GetQueryInfo(table, Storage);
            TableInfo rootTable = table;
            while (rootTable.Parent != null)
            {
                rootTable = rootTable.Parent;
            }
            // TODO change by where when ready
            string sql = info.sql + " WHERE " + rootTable.SqlTableName + ".id = " + id;
            ResultWithError<DbCommand> cmdResult = Storage.CreateCmd(sql);
            result.Errors.AddRange(cmdResult.Errors);
            if (!result.Success || cmdResult.Result == null)
            {
                return result;
            }
            DbCommand cmd = cmdResult.Result;
            StorageQueryResult queryResult = Storage.Query(cmd, null);
            cmd.Dispose();
            result.Errors.AddRange(queryResult.Errors);
            if (queryResult.Success)
            {
                if (queryResult.Result.Count > 1)
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, "There are more than one result inside getById"));
                }
                else
                {
                    foreach (Dictionary<string, string> item in queryResult.Result)
                    {
                        ResultWithError<X> resultObjTemp = Utils.CreateObject<X>(Storage, item, info.memberInfos);
                        result.Errors.AddRange(resultObjTemp.Errors);
                        if (resultObjTemp.Result != null)
                        {
                            result.Result = resultObjTemp.Result;
                        }
                    }
                }
            }
            return result;
        }
    }
}
