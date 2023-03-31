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
        public override ResultWithError<List<X>> run<X>(TableInfo table)
        {
            // create a query for all the hierarchy

            // query for animal
            // SELECT _type, animal.id, animal.name, animal.createdDate, animal.updatedDate FROM animal
            // left outer join dog on animal.id = dog.id
            // left outer join felin on animal.id = felin.id
            // left outer join cat on felin.id = cat.id

            // query for felin
            // SELECT _type, animal.id, animal.name, animal.createdDate, animal.updatedDate FROM animal
            // inner join felin on animal.id = felin.id
            // left outer join cat on felin.id = cat.id

            // query for cat
            // SELECT _type, animal.id, animal.name, animal.createdDate, animal.updatedDate FROM animal
            // inner join felin on animal.id = felin.id
            // inner join cat on felin.id = cat.id

            // query for dog
            // SELECT _type, animal.id, animal.name, animal.createdDate, animal.updatedDate FROM animal
            // inner join dog on animal.id = dog.id
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Not implemented"));
            return result;
        }

        
    }
}
