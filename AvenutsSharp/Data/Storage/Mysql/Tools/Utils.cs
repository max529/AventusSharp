using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Tools
{
    internal static class Utils
    {
        public static string? GetIntermediateTablename(TableMemberInfo member)
        {
            TableInfo from = member.TableInfo;
            TableInfo? to = member.TableLinked;
            if(to == null)
            {
                return null;
            }
            return from.SqlTableName + "_" + to.SqlTableName + "_" + member.SqlName + "_link";
        }

        private static Random random = new Random();
        public static string CheckConstraint(string constraint)
        {
            if (constraint.Length > 128)
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                return new string(Enumerable.Repeat(chars, 128).Select(s => s[random.Next(s.Length)]).ToArray());
            }
            return constraint;
        }


        public static ResultWithError<X> CreateObject<X>(IStorage storage, Dictionary<string, string> itemFields, List<TableMemberInfo> memberInfos)
        {
            ResultWithError<X> result = new ResultWithError<X>();
            Type type = typeof(X);
            TableInfo? info = storage.GetTableInfo(type);
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
