using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Delete;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;

namespace AventusSharp.Data.Storage.Mysql.Queries
{

    internal class Delete
    {
        private class DeleteQueryInfoTemp
        {
            public List<string> conditions = new();
            public List<DbParameter> parametersSQL = new();
            public Dictionary<string, TableMemberInfo> memberByParameters = new();

        }

        private static void LoadMembers(TableInfo table, DeleteQueryInfoTemp info, MySQLStorage storage)
        {
            foreach (TableMemberInfo member in table.Members)
            {
                if (member.IsPrimary)
                {
                    string paramName = "@" + member.SqlName;
                    info.conditions.Add(member.SqlName + "=" + paramName);
                    info.memberByParameters.Add(paramName, member);

                    DbParameter parameter = storage.GetDbParameter();
                    parameter.ParameterName = paramName;
                    parameter.DbType = member.SqlType;
                    info.parametersSQL.Add(parameter);
                }
            }
        }
        public static DeleteQueryInfo CreateQueryInfo(TableInfo table, MySQLStorage storage)
        {
            string sql = "DELETE FROM " + table.SqlTableName + " WHERE $condition";

            DeleteQueryInfoTemp infoTemp = new();

            LoadMembers(table, infoTemp, storage);

            sql = sql.Replace("$condition", string.Join(" AND ", infoTemp.conditions));

            List<Dictionary<string, object?>> func(IList data)
            {
                List<Dictionary<string, object?>> result = new();
                foreach (object item in data)
                {
                    Dictionary<string, object?> line = new();
                    foreach (KeyValuePair<string, TableMemberInfo> param in infoTemp.memberByParameters)
                    {
                        line.Add(param.Key, param.Value.GetSqlValue(item));
                    }
                    result.Add(line);
                }

                return result;
            }

            Console.WriteLine(sql);

            DeleteQueryInfo infoFinal = new(
                sql: sql,
                getParams: func,
                parameters: infoTemp.parametersSQL
            );

            return infoFinal;
        }



        public static string PrepareSQL<X>(DatabaseDeleteBuilder<X> deleteBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseBuilderInfo mainInfo = deleteBuilder.InfoByPath[""];
            List<string> joins = new();
            List<string> aliases = new();

            void loadInfo(DatabaseBuilderInfo baseInfo)
            {
                string lastAlias = baseInfo.Alias;
                TableInfo lastTableInfo = baseInfo.TableInfo;
                aliases.Insert(0, lastAlias + ".*");
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.Parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;

                    joins.Add("INNER JOIN " + info.SqlTableName + " " + alias + " ON " + lastAlias + "." + lastTableInfo.Primary?.SqlName + "=" + alias + "." + info.Primary?.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                    aliases.Insert(0, lastAlias + ".*");
                }
            }

            loadInfo(mainInfo);


            string whereTxt = "";
            if (deleteBuilder.Wheres != null)
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
                            subQuery += "@" + paramConst.Value;
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

                foreach (WhereGroup whereGroup in deleteBuilder.Wheres)
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

            string sql = "DELETE " + string.Join(",", aliases) + " FROM " + mainInfo.TableInfo.SqlTableName + " " + mainInfo.Alias
                + joinTxt
                + whereTxt;


            Console.WriteLine(sql);
            return sql;
        }

    }
}
