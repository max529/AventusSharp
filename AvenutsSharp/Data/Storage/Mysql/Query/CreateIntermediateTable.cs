using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Query
{
    internal class CreateIntermediateTable
    {
        public static string GetQuery(TableMemberInfo memberInfo)
        {
            TableInfo instance = memberInfo.TableInfo;
            TableInfo link = memberInfo.TableLinked;

            string intermediateTableName = Utils.GetIntermediateTablename(memberInfo);

            List<string> schema = new List<string>();
            List<string> primaryConstraint = new List<string>();
            List<string> foreignConstraint = new List<string>();
            string separator = ",\r\n";

            List<TableMemberInfo> prims = instance.members.Where(f => f.IsPrimary).ToList();
            List<string> primsName = prims.Select(prim => prim.SqlName).ToList();
            List<string> intermediatePrimsNameTmp = new List<string>();
            foreach (TableMemberInfo prim in prims)
            {
                string intermediateName = "`" + prim.SqlName + "_" + instance.SqlTableName + "`";
                string schemaProp = "\t" + intermediateName + " " + prim.SqlTypeTxt;

                intermediatePrimsNameTmp.Add(intermediateName);
                primaryConstraint.Add(intermediateName);
                schema.Add(schemaProp);
            }
            string constraintName = "FK_" + string.Join("_", intermediatePrimsNameTmp).Replace("`", "").Replace("`", "") + "_" + intermediateTableName + "_" + instance.SqlTableName;
            constraintName = Utils.CheckConstraint(constraintName);
            string constraintProp = "\t" + "CONSTRAINT `" + constraintName + "` FOREIGN KEY (" + string.Join(", ", intermediatePrimsNameTmp) + ") REFERENCES `" + instance.SqlTableName + "` (" + string.Join(", ", primsName) + ")";

            foreignConstraint.Add(constraintProp);


            prims = link.members.Where(f => f.IsPrimary).ToList();
            primsName = prims.Select(prim => prim.SqlName).ToList();
            intermediatePrimsNameTmp = new List<string>();
            foreach (TableMemberInfo prim in prims)
            {
                //member.name usefull if link is same class as instance
                string intermediateName = "`" + memberInfo.SqlName + "*" + prim.SqlName + "_" + link.SqlTableName + "`";
                string schemaProp = "\t" + intermediateName + " " + prim.SqlTypeTxt;

                intermediatePrimsNameTmp.Add(intermediateName);
                primaryConstraint.Add(intermediateName);
                schema.Add(schemaProp);
            }

            constraintName = "FK_" + string.Join("_", intermediatePrimsNameTmp).Replace("`", "").Replace("`", "") + "_" + intermediateTableName + "_" + link.SqlTableName;
            constraintName = Utils.CheckConstraint(constraintName);
            constraintProp = "\t" + "CONSTRAINT `" + constraintName + "` FOREIGN KEY (" + string.Join(", ", intermediatePrimsNameTmp) + ") REFERENCES `" + link.SqlTableName + "` (" + string.Join(", ", primsName) + ")";

            foreignConstraint.Add(constraintProp);


            string sql = "CREATE TABLE " + intermediateTableName + " (\r\n";
            sql += string.Join(separator, schema);
            if (primaryConstraint.Count > 0)
            {
                sql += separator;
                string joinedPrimary = string.Join(",", primaryConstraint);
                string primaryProp = "\tCONSTRAINT PK_" + intermediateTableName + " PRIMARY KEY (" + joinedPrimary + ")";
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
