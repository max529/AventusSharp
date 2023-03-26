using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class UpdateAction : UpdateAction<MySQLStorage>
    {
        public override ResultWithError<List<X>> run<X>(TableInfo table, List<X> data)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Not implemented"));
            return result;
        }
    }
}
