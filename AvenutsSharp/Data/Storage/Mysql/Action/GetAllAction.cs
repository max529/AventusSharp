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
    internal class GetAllAction : GetAllAction<MySQLStorage>
    {
        public override ResultWithError<List<X>> run<X>(TableInfo table)
        {

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Result = new List<X>();
            GetAllQueryInfo info = GetAll.GetQueryInfo(table, Storage);

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
                    ResultWithError<X> resultObjTemp = createObject<X>(item, info.memberInfos);
                    result.Errors.AddRange(resultObjTemp.Errors);
                    if(resultObjTemp.Result != null)
                    {
                        result.Result.Add(resultObjTemp.Result);
                    }
                }
            }

            return result;

        }

        private ResultWithError<X> createObject<X>(Dictionary<string, string> itemFields, List<TableMemberInfo> memberInfos)
        {
            ResultWithError<X> result = new ResultWithError<X>();
            Type type = typeof(X);
            TableInfo? info = Storage.getTableInfo(type);
            if (info == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type.Name));
                return result;
            }
            object o;
            if (info.IsAbstract)
            {
                if (!itemFields.ContainsKey(TableInfo.TypeIdentifierName))
                {
                    result.Errors.Add(new DataError(DataErrorCode.NoTypeIdentifierFoundInsideQuery, "Can't find the field " + TableInfo.TypeIdentifierName));
                    return result;
                }

                Type? typeToCreate = Type.GetType(itemFields[TableInfo.TypeIdentifierName]);
                if (typeToCreate == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.WrongType, "Can't find the type " + itemFields[TableInfo.TypeIdentifierName]));
                    return result;
                }

                o = TypeTools.CreateNewObj(typeToCreate);
            }
            else
            {
                o = TypeTools.CreateNewObj(type);
            }

            if (o is X oCasted)
            {
                foreach (TableMemberInfo memberInfo in memberInfos)
                {
                    if (itemFields.ContainsKey(memberInfo.SqlName))
                    {
                        memberInfo.SetSqlValue(o, itemFields[memberInfo.SqlName]);
                    }
                }
                result.Result = oCasted;
            }

            return result;
        }
    }
}
