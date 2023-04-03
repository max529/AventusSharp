using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Query
{

    internal class GetAllQueryInfo
    {
        public string sql { get; set; }
    }
    internal class GetAll
    {
        private static Dictionary<TableInfo, GetAllQueryInfo> queriesInfo = new Dictionary<TableInfo, GetAllQueryInfo>();

        private class GetAllQueryInfoTemp
        {
            public List<string> fields = new List<string>();
            public List<string> join = new List<string>();

        }

        public static GetAllQueryInfo GetQueryInfo(TableInfo tableInfo, MySQLStorage storage)
        {
            if (!queriesInfo.ContainsKey(tableInfo))
            {
                createQueryInfo(tableInfo, storage);
            }
            return queriesInfo[tableInfo];
        }

        private static void loadMembers(TableInfo table, GetAllQueryInfoTemp info, MySQLStorage storage)
        {
            foreach (TableMemberInfo member in table.members)
            {
                if (member.link != TableMemberInfoLink.Multiple)
                {
                    if (member.link != TableMemberInfoLink.Parent)
                    {
                        info.fields.Add(table.SqlTableName + "." + member.SqlName);
                    }
                    else
                    {
                        string join = "left outer join " + member.TableLinked.SqlTableName + " on " + table.SqlTableName + "." + member.SqlName + " = " + member.TableLinked.SqlTableName + "." + member.SqlName;
                        info.join.Add(join);
                    }
                }
            }
        }


        private static void createQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "SELECT $fields FROM " + table.SqlTableName + " WHERE $condition";

            GetAllQueryInfoTemp infoTemp = new GetAllQueryInfoTemp();

            loadMembers(table, infoTemp, storage);

            sql = sql.Replace("$fields", string.Join(", ", infoTemp.fields));


            Console.WriteLine(sql);

            GetAllQueryInfo infoFinal = new GetAllQueryInfo()
            {
                sql = sql,
            };

            queriesInfo[table] = infoFinal;
        }

    }
}
