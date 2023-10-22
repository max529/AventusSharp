using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    internal class CreateIntermediateTable
    {
        public static string GetQuery(TableMemberInfo memberInfo)
        {
            TableInfo instance = memberInfo.TableInfo;
            TableInfo? link = memberInfo.TableLinked;
            if(link == null)
            {
                return "";
            }

            string? intermediateTableName = Utils.GetIntermediateTablename(memberInfo);
            if(intermediateTableName == null || instance.Primary == null || link.Primary == null)
            {
                return "";
            }

            List<string> schema = new();
            List<string> primaryConstraint = new();
            List<string> foreignConstraint = new();
            string separator = ",\r\n";

            string intermediateName = "`" + instance.SqlTableName + "_" + instance.Primary.SqlName + "`";
            string schemaProp = "\t" + intermediateName + " " + instance.Primary.SqlTypeTxt;
            string constraintName = "`FK_" + intermediateTableName + "_" + instance.SqlTableName+"`";

            primaryConstraint.Add(intermediateName);
            schema.Add(schemaProp);
            
            constraintName = Utils.CheckConstraint(constraintName);
            string constraintProp = "\t" + "CONSTRAINT " + constraintName + " FOREIGN KEY (" + intermediateName + ") REFERENCES `" + instance.SqlTableName + "` (" + instance.Primary.SqlName + ")";
            foreignConstraint.Add(constraintProp);



            intermediateName = "`" + link.SqlTableName + "_" + link.Primary.SqlName + "`";
            schemaProp = "\t" + intermediateName + " " + link.Primary.SqlTypeTxt;
            constraintName = "`FK_" + intermediateTableName + "_" + link.SqlTableName + "`";

            primaryConstraint.Add(intermediateName);
            schema.Add(schemaProp);

            constraintName = Utils.CheckConstraint(constraintName);
            constraintProp = "\t" + "CONSTRAINT " + constraintName + " FOREIGN KEY (" + intermediateName + ") REFERENCES `" + link.SqlTableName + "` (" + link.Primary.SqlName + ")";
            foreignConstraint.Add(constraintProp);
            


            string sql = "CREATE TABLE `" + intermediateTableName + "` (\r\n";
            sql += string.Join(separator, schema);
            if (primaryConstraint.Count > 0)
            {
                sql += separator;
                string joinedPrimary = string.Join(",", primaryConstraint);
                string primaryProp = "\tCONSTRAINT `PK_" + intermediateTableName + "` PRIMARY KEY (" + joinedPrimary + ")";
                sql += primaryProp;
            }
            if (foreignConstraint.Count > 0)
            {
                sql += separator;
                sql += string.Join(separator, foreignConstraint);
            }
            sql += ")";

            return sql;
        }
    }
}
