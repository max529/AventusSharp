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
                    joins.Add("INNER JOIN " + info.SqlTableName + " " + alias + " ON " + lastAlias + "." + lastTableInfo.Primary?.SqlName + "=" + alias + "." + info.Primary?.SqlName);
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
                        joins.Add("LEFT OUTER JOIN " + child.TableInfo.SqlTableName + " " + child.Alias + " ON " + parentAlias + "." + parentPrimName + "=" + alias + "." + primName);
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
                    joins.Add("LEFT OUTER JOIN " + databaseQueryBuilderInfo.TableInfo.SqlTableName + " " + databaseQueryBuilderInfo.Alias + " ON " + baseInfo.Alias + "." + tableMemberInfo.SqlName + "=" + databaseQueryBuilderInfo.Alias + "." + databaseQueryBuilderInfo.TableInfo.Primary?.SqlName);
                    path.Add(tableMemberInfo.Name);
                    types.Add(tableMemberInfo.Type);
                    loadInfo(databaseQueryBuilderInfo, path, types);
                    path.RemoveAt(path.Count - 1);
                    types.RemoveAt(types.Count - 1);

                }
            }

            loadInfo(mainInfo, new List<string>(), new List<Type>());

            string whereTxt = "";
            if (queryBuilder.Wheres != null)
            {
                string buildWhere(WhereGroup whereGroup, string whereTxt)
                {
                    whereTxt += "(";
                    string subQuery = "";
                    IWhereGroup? lastGroup = null;
                    foreach (IWhereGroup queryGroup in whereGroup.Groups)
                    {
                        if (queryGroup is WhereGroup childWhereGroup)
                        {
                            subQuery += buildWhere(childWhereGroup, "");
                        }
                        else if (queryGroup is WhereGroupFct fctGroup)
                        {
                            subQuery += storage.GetFctName(fctGroup.Fct);
                        }
                        else if (queryGroup is WhereGroupConstantNull nullConst)
                        {
                            // special case for IS and IS NOT
                            if (whereGroup.Groups.Count == 3)
                            {
                                WhereGroupFct? fctGrp = null;
                                WhereGroupField? fieldGrp = null;
                                for (int i = 0; i < whereGroup.Groups.Count; i++)
                                {
                                    if (whereGroup.Groups[i] is WhereGroupFct fctGrpTemp && (fctGrpTemp.Fct == WhereGroupFctEnum.Equal || fctGrpTemp.Fct == WhereGroupFctEnum.NotEqual))
                                    {
                                        fctGrp = fctGrpTemp;
                                    }
                                    else if (whereGroup.Groups[i] is WhereGroupField fieldGrpTemp)
                                    {
                                        fieldGrp = fieldGrpTemp;
                                    }
                                }

                                if (fctGrp != null && fieldGrp != null)
                                {
                                    string action = fctGrp.Fct == WhereGroupFctEnum.Equal ? " IS NULL" : " IS NOT NULL";
                                    subQuery = fieldGrp.Alias + "." + fieldGrp.TableMemberInfo.SqlName + action;
                                    break;
                                }
                            }

                            subQuery += "NULL";
                        }
                        else if (queryGroup is WhereGroupConstantBool boolConst)
                        {
                            subQuery += boolConst.Value ? "1" : "0";
                        }
                        else if (queryGroup is WhereGroupConstantString stringConst)
                        {
                            string strValue = "'" + stringConst.Value + "'";
                            if (lastGroup is WhereGroupFct groupFct)
                            {
                                if (groupFct.Fct == WhereGroupFctEnum.StartsWith)
                                {
                                    strValue = "'" + stringConst.Value + "%'";
                                }
                                else if (groupFct.Fct == WhereGroupFctEnum.EndsWith)
                                {
                                    strValue = "'%" + stringConst.Value + "'";
                                }
                                else if (groupFct.Fct == WhereGroupFctEnum.ContainsStr)
                                {
                                    strValue = "'%" + stringConst.Value + "%'";
                                }
                            }
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereGroupConstantDateTime dateTimeConst)
                        {
                            subQuery += "'" + dateTimeConst.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        }
                        else if (queryGroup is WhereGroupConstantOther otherConst)
                        {
                            subQuery += otherConst.Value;
                        }
                        else if (queryGroup is WhereGroupConstantParameter paramConst)
                        {
                            string strValue = "@" + paramConst.Value;
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereGroupField fieldGrp)
                        {
                            subQuery += fieldGrp.Alias + "." + fieldGrp.TableMemberInfo.SqlName;
                        }
                        lastGroup = queryGroup;
                    }
                    whereTxt += subQuery;
                    whereTxt += ")";
                    return whereTxt;
                }

                foreach (WhereGroup whereGroup in queryBuilder.Wheres)
                {
                    whereTxt += buildWhere(whereGroup, whereTxt);
                }
                if (whereTxt.Length > 1)
                {
                    whereTxt = " WHERE " + whereTxt;
                }
            }

            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            string sql = "SELECT " + string.Join(",", fields)
                + " FROM " + mainInfo.TableInfo.SqlTableName + " " + mainInfo.Alias
                + joinTxt
                + whereTxt;


            Console.WriteLine(sql);
            return sql;
        }
    }
}
