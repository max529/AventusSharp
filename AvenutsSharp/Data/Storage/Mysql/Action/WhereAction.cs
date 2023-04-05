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
    internal class WhereAction : WhereAction<MySQLStorage>
    {
        public override ResultWithError<List<X>> run<X>(TableInfo table, Expression<Func<X, bool>> func)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();
            WhereQueryInfo info = Where.GetQueryInfo(table, (BinaryExpression)func.Body, Storage);

            ResultWithError<DbCommand> cmdResult = Storage.CreateCmd(info.sql);
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
                foreach (Dictionary<string, string> item in queryResult.Result)
                {
                    ResultWithError<X> resultObjTemp = Utils.CreateObject<X>(Storage, item, info.memberInfos);
                    result.Errors.AddRange(resultObjTemp.Errors);
                    if (resultObjTemp.Result != null)
                    {
                        result.Result.Add(resultObjTemp.Result);
                    }
                }
            }

            return result;
        }
    }
}
