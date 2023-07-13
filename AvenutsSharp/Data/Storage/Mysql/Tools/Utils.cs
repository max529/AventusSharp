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

        private static readonly Random random = new();
        public static string CheckConstraint(string constraint)
        {
            if (constraint.Length > 128)
            {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                return new string(Enumerable.Repeat(chars, 128).Select(s => s[random.Next(s.Length)]).ToArray());
            }
            return constraint;
        }
    }
}
