using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Create;
using AventusSharp.Data.Storage.Default;
using System.Collections.Generic;

namespace AventusSharp.Data.Storage.Mysql.Queries
{

    internal class Create
    {
        public static List<DatabaseCreateBuilderInfo> PrepareSQL<X>(DatabaseCreateBuilder<X> createBuilder) where X : IStorable
        {
            List<DatabaseCreateBuilderInfo> result = new();

            void createSql(TableInfo tableInfo)
            {
                List<ParamsInfo> paramsInfos = new();
                List<string> columns = new();
                List<string> values = new();
                bool hasPrimaryResult = tableInfo.Parent == null;
                ParamsInfo? primaryToSet = null;
                foreach (TableMemberInfo member in tableInfo.Members)
                {
                    if (member.Link != TableMemberInfoLink.Multiple)
                    {
                        if (member.IsAutoIncrement)
                        {
                            continue;
                        }
                        ParamsInfo paramsInfo = new()
                        {
                            DbType = member.SqlType,
                            Name = member.SqlName,
                            TypeLvl0 = tableInfo.Type,
                            MembersList = new List<TableMemberInfo>() { member }
                        };
                        if (!hasPrimaryResult && member.IsPrimary)
                        {
                            primaryToSet = paramsInfo;
                        }
                        else
                        {
                            paramsInfos.Add(paramsInfo);
                        }
                        columns.Add(member.SqlName);
                        values.Add("@" + member.SqlName);
                    }
                }

                string sql = $"INSERT INTO {tableInfo.SqlTableName} ({string.Join(",", columns)}) VALUES ({string.Join(",", values)});";

                if (hasPrimaryResult)
                {
                    sql += "SELECT last_insert_id() as id;";
                }
                DatabaseCreateBuilderInfo resultTemp = new(sql, hasPrimaryResult, paramsInfos)
                {
                    PrimaryToSet = primaryToSet
                };
                if (hasPrimaryResult && tableInfo.Primary != null)
                {
                    createBuilder.PrimaryParam = new ParamsInfo()
                    {
                        DbType = tableInfo.Primary.SqlType,
                        Name = tableInfo.Primary.SqlName,
                        TypeLvl0 = tableInfo.Type,
                        MembersList = new List<TableMemberInfo>() { tableInfo.Primary }
                    };
                }
                if (tableInfo.Parent != null)
                {
                    createSql(tableInfo.Parent);
                }
                result.Add(resultTemp);

            };

            createSql(createBuilder.TableInfo);

            return result;
        }

    }
}
