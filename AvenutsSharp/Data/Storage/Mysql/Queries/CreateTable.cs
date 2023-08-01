using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    internal class CreateTable
    {
        public static string GetQuery(TableInfo table)
        {
            string sql = "CREATE TABLE `" + table.SqlTableName + "` (\r\n";

            List<string> schema = new();
            List<string> primaryConstraint = new();
            List<string> foreignConstraint = new();
            string separator = ",\r\n";

            // key is sql_table_name
            Dictionary<string, Dictionary<string, List<TableMemberInfo>>> primariesByClass = new();

            foreach (TableMemberInfo member in table.Members)
            {
                if (member.Link != TableMemberInfoLink.Multiple)
                {
                    string schemaProp = "\t`" + member.SqlName + "` " + member.SqlTypeTxt;
                    if (!member.IsNullable)
                    {
                        schemaProp += " NOT NULL";
                    }
                    if (member.IsAutoIncrement)
                    {
                        schemaProp += " AUTO_INCREMENT";
                    }
                    schema.Add(schemaProp);

                    if (member.IsPrimary)
                    {
                        primaryConstraint.Add("`" + member.SqlName + "`");
                    }

                    if (member.Link == TableMemberInfoLink.Simple || member.Link == TableMemberInfoLink.Parent)
                    {
                        if (member.TableLinked != null)
                        {
                            if (!primariesByClass.ContainsKey(member.TableLinked.SqlTableName))
                            {
                                primariesByClass[member.TableLinked.SqlTableName] = new Dictionary<string, List<TableMemberInfo>>();
                            }
                            if (!primariesByClass[member.TableLinked.SqlTableName].ContainsKey(member.Name))
                            {
                                primariesByClass[member.TableLinked.SqlTableName][member.Name] = new List<TableMemberInfo>();
                            }
                            primariesByClass[member.TableLinked.SqlTableName][member.Name].Add(member);
                        }
                        else
                        {
                            // TODO code external link
                        }
                    }
                }

            }

            // There is only one constraint by class for foreignkey (if many primaries into foreign class)
            foreach (KeyValuePair<string, Dictionary<string, List<TableMemberInfo>>> primary in primariesByClass)
            {
                foreach (KeyValuePair<string, List<TableMemberInfo>> pri in primary.Value)
                {
                    bool deleteOnCascade = pri.Value.FirstOrDefault(p => p.IsDeleteOnCascade) != null;
                    string constraintName = "FK_" + string.Join("_", pri.Value.Select(field => field.SqlName)) + "_" + table.SqlTableName + "_" + primary.Key;
                    constraintName = Utils.CheckConstraint(constraintName);
                    string constraintProp = "\t" + "CONSTRAINT `" + constraintName + "` FOREIGN KEY (" + string.Join(", ", pri.Value.Select(field => "`" + field.SqlName + "`")) + ") REFERENCES `" + primary.Key + "` (" + string.Join(", ", pri.Value.Select(field => "`" + field.TableLinked?.Primary?.SqlName + "`")) + ")";
                    if(deleteOnCascade)
                    {
                        // TODO pour les tests mais doit être calculé du côté manager (seulement si stocker dans la RAM?)
                        constraintProp += " ON DELETE CASCADE";
                    }
                    foreignConstraint.Add(constraintProp);
                }

            }

            sql += string.Join(separator, schema);
            if (primaryConstraint.Count > 0)
            {
                sql += separator;
                string joinedPrimary = string.Join(",", primaryConstraint);
                string primaryProp = "\tCONSTRAINT `PK_" + table.SqlTableName + "` PRIMARY KEY (" + joinedPrimary + ")";
                sql += primaryProp;
            }
            if (foreignConstraint.Count > 0)
            {
                sql += separator;
                sql += string.Join(separator, foreignConstraint);
            }
            sql += ")";
            Console.WriteLine(sql);
            return sql;
        }

        
    }
}
