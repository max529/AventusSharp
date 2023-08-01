using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    public class Update
    {
        public static string PrepareSQL<X>(DatabaseUpdateBuilder<X> updateBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseBuilderInfo mainInfo = updateBuilder.InfoByPath[""];
            List<string> fields = new();
            List<string> joins = new();

            void loadInfo(DatabaseBuilderInfo baseInfo)
            {
                if (updateBuilder.AllFieldsUpdate)
                {
                    LoadTableFieldUpdate(baseInfo.TableInfo, baseInfo.Alias, updateBuilder.UpdateParamsInfo);
                }
                string lastAlias = baseInfo.Alias;
                TableInfo lastTableInfo = baseInfo.TableInfo;
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.Parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;
                    if (updateBuilder.AllFieldsUpdate)
                    {
                        LoadTableFieldUpdate(info, alias, updateBuilder.UpdateParamsInfo);
                    }
                    joins.Add("INNER JOIN `" + info.SqlTableName + "` " + alias + " ON " + lastAlias + "." + lastTableInfo.Primary?.SqlName + "=" + alias + "." + info.Primary?.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                }
            }

            loadInfo(mainInfo);

            foreach (KeyValuePair<string, ParamsInfo> paramInfo in updateBuilder.UpdateParamsInfo)
            {
                string name = paramInfo.Key;
                fields.Add(name + " = @" + name);
            }

            string whereTxt = "";
            if (updateBuilder.Wheres != null)
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

                foreach (WhereGroup whereGroup in updateBuilder.Wheres)
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

            string sql = "UPDATE `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias
                + joinTxt
                + " SET " + string.Join(",", fields)
                + whereTxt;


            Console.WriteLine(sql);
            KeyValuePair<TableMemberInfo?, string> pair = updateBuilder.InfoByPath[""].GetTableMemberInfoAndAlias("id");
            if(pair.Key == null)
            {
                throw new Exception("Can't find Id... 0_o");
            }
            string idField = pair.Value + "." + pair.Key.SqlName;
            updateBuilder.SqlQuery = "SELECT " + idField + " FROM `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias + joinTxt + whereTxt;
            return sql;
        }

        private static void LoadTableFieldUpdate(TableInfo tableInfo, string alias, Dictionary<string, ParamsInfo> updateParamsInfo)
        {
            foreach (TableMemberInfo member in tableInfo.Members)
            {
                if (member.Link != TableMemberInfoLink.Multiple)
                {
                    if (member.Link != TableMemberInfoLink.Parent && member.IsUpdatable)
                    {
                        string name = alias + "." + member.SqlName;
                        updateParamsInfo.Add(name, new ParamsInfo()
                        {
                            DbType = member.SqlType,
                            Name = name,
                            TypeLvl0 = tableInfo.Type,
                            MembersList = new List<TableMemberInfo>() { member }
                        });
                    }
                }
            }
        }

    }
}
