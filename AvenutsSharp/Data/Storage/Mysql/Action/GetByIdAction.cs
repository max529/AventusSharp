using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class GetByIdAction : GetByIdAction<MySQLStorage>
    {
        public override ResultWithError<X> run<X>(TableInfo table, int id)
        {
            ResultWithError<X> result = new ResultWithError<X>();
            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Not implemented"));
            return result;
        }
    }
}
