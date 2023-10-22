using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    public class Query
    {
        public static string PrepareSQL<X>(DatabaseQueryBuilder<X> queryBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseBuilderInfo mainInfo = queryBuilder.InfoByPath[""];
            List<string> fields = new();
            List<string> joins = new();

            void loadInfo(DatabaseBuilderInfo baseInfo, List<string> path, List<Type> types)
            {

                if (queryBuilder.AllMembers)
                {
                    storage.LoadTableFieldQuery(baseInfo.TableInfo, baseInfo.Alias, baseInfo, path, types, queryBuilder);
                }
                else if (baseInfo.TableInfo.TypeMember != null)
                {
                    TableMemberInfo member = baseInfo.TableInfo.TypeMember;
                    string alias = baseInfo.Alias;
                    fields.Add(alias + "." + member.SqlName + " `" + alias + "*" + member.SqlName + "`");
                }
                string lastAlias = baseInfo.Alias;
                TableInfo lastTableInfo = baseInfo.TableInfo;
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.Parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;
                    if (queryBuilder.AllMembers)
                    {
                        storage.LoadTableFieldQuery(info, alias, baseInfo, path, types, queryBuilder);
                    }
                    else if (parentLink.Key.TypeMember != null)
                    {
                        TableMemberInfo member = parentLink.Key.TypeMember;
                        fields.Add(alias + "." + member.SqlName + " `" + alias + "*" + member.SqlName + "`");
                    }
                    joins.Add("INNER JOIN `" + info.SqlTableName + "` " + alias + " ON " + lastAlias + "." + lastTableInfo.Primary?.SqlName + "=" + alias + "." + info.Primary?.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                }

                Action<List<DatabaseBuilderInfoChild>, string, string?> loadChild = (children, parentAlias, parentPrimName) => { };
                loadChild = (children, parentAlias, parentPrimName) =>
                {
                    foreach (DatabaseBuilderInfoChild child in children)
                    {
                        string alias = child.Alias;
                        string primName = child.TableInfo.Primary?.SqlName ?? "";
                        if (queryBuilder.AllMembers)
                        {
                            storage.LoadTableFieldQuery(child.TableInfo, alias, baseInfo, path, types, queryBuilder);
                        }
                        else if (child.TableInfo.TypeMember != null)
                        {
                            TableMemberInfo member = child.TableInfo.TypeMember;
                            fields.Add(alias + "." + member.SqlName + " `" + alias + "*" + member.SqlName + "`");
                        }
                        joins.Add("LEFT OUTER JOIN `" + child.TableInfo.SqlTableName + "` " + child.Alias + " ON " + parentAlias + "." + parentPrimName + "=" + alias + "." + primName);
                        loadChild(child.Children, alias, primName);
                    }
                };
                loadChild(baseInfo.Children, baseInfo.Alias, baseInfo.TableInfo.Primary?.SqlName);

                foreach (KeyValuePair<TableMemberInfo, DatabaseBuilderInfoMember> member in baseInfo.Members)
                {
                    string alias = member.Value.Alias;
                    fields.Add(alias + "." + member.Key.SqlName + " `" + alias + "*" + member.Key.SqlName + "`");
                }

                foreach (KeyValuePair<TableMemberInfo, DatabaseBuilderInfo> linkInfo in baseInfo.links)
                {
                    TableMemberInfo tableMemberInfo = linkInfo.Key;
                    DatabaseBuilderInfo databaseQueryBuilderInfo = linkInfo.Value;
                    if (tableMemberInfo.Type == null)
                    {
                        continue;
                    }
                    joins.Add("LEFT OUTER JOIN `" + databaseQueryBuilderInfo.TableInfo.SqlTableName + "` " + databaseQueryBuilderInfo.Alias + " ON " + baseInfo.Alias + "." + tableMemberInfo.SqlName + "=" + databaseQueryBuilderInfo.Alias + "." + databaseQueryBuilderInfo.TableInfo.Primary?.SqlName);
                    path.Add(tableMemberInfo.Name);
                    types.Add(tableMemberInfo.Type);
                    loadInfo(databaseQueryBuilderInfo, path, types);
                    path.RemoveAt(path.Count - 1);
                    types.RemoveAt(types.Count - 1);

                }
            }

            loadInfo(mainInfo, new List<string>(), new List<Type>());

            string whereTxt = BuilderTools.Where(queryBuilder.Wheres);
            
            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            string sql = "SELECT " + string.Join(",", fields)
                + " FROM `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias
                + joinTxt
                + whereTxt;


            return sql;
        }
    }
}
