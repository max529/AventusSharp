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
        public string sql { get; set; } = "";
        public List<TableMemberInfo> memberInfos { get; set; } = new List<TableMemberInfo>();
    }
    internal class GetAll
    {
        private static Dictionary<TableInfo, GetAllQueryInfo> queriesInfo = new Dictionary<TableInfo, GetAllQueryInfo>();

        private class GetAllQueryInfoTemp
        {
            public List<string> fields = new List<string>();
            public List<string> join = new List<string>();
            public List<TableMemberInfo> memberInfos = new List<TableMemberInfo>();

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
                        info.memberInfos.Add(member);
                    }
                    else
                    {
                        string join = "inner join " + member.TableLinked?.SqlTableName + " on " + table.SqlTableName + "." + member.SqlName + " = " + member.TableLinked?.SqlTableName + "." + member.SqlName;
                        info.join.Add(join);
                    }
                }
            }
        }
        private static void loadParent(TableInfo table, GetAllQueryInfoTemp info, MySQLStorage storage)
        {
            if (table.Parent != null)
            {
                TableInfo parent = table.Parent;
                foreach (TableMemberInfo member in parent.members)
                {
                    if (member.link != TableMemberInfoLink.Multiple)
                    {
                        if (member.link != TableMemberInfoLink.Parent)
                        {
                            info.fields.Add(parent.SqlTableName + "." + member.SqlName);
                            info.memberInfos.Add(member);
                        }
                        else
                        {
                            string join = "inner join " + member.TableInfo.SqlTableName + " on " + parent.SqlTableName + "." + member.SqlName + " = " + member.TableLinked?.SqlTableName + "." + member.SqlName;
                            info.join.Add(join);
                        }
                    }
                }
                loadParent(parent, info, storage);
            }

        }
        private static void loadChildren(TableInfo table, GetAllQueryInfoTemp info, MySQLStorage storage)
        {
            foreach (TableInfo child in table.Children)
            {
                foreach (TableMemberInfo member in child.members)
                {
                    if (member.link != TableMemberInfoLink.Multiple)
                    {
                        if (member.link != TableMemberInfoLink.Parent)
                        {
                            info.fields.Add(child.SqlTableName + "." + member.SqlName);
                            info.memberInfos.Add(member);
                        }
                        else
                        {
                            string join = "left outer join " + child.SqlTableName + " on " + child.SqlTableName + "." + member.SqlName + " = " + member.TableLinked?.SqlTableName + "." + member.SqlName;
                            info.join.Add(join);
                        }
                    }
                }
                loadChildren(child, info, storage);
            }
        }

        private static void createQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "SELECT $fields FROM " + table.SqlTableName;

            GetAllQueryInfoTemp infoTemp = new GetAllQueryInfoTemp();

            loadMembers(table, infoTemp, storage);
            loadParent(table, infoTemp, storage);
            loadChildren(table, infoTemp, storage);

            sql = sql.Replace("$fields", string.Join(", ", infoTemp.fields));

            if (infoTemp.join.Count > 0)
            {
                sql += " " + string.Join(" ", infoTemp.join);
            }



            Console.WriteLine(sql);

            GetAllQueryInfo infoFinal = new GetAllQueryInfo()
            {
                sql = sql,
                memberInfos = infoTemp.memberInfos
            };

            queriesInfo[table] = infoFinal;
        }

    }
}
