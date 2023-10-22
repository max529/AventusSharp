using AventusSharp.Data.Manager.DB;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    public static class BuilderTools
    {
        public static string Where(List<WhereGroup>? wheres)
        {
            if(wheres == null)
            {
                return "";
            }
            string whereTxt = "";
            foreach (WhereGroup whereGroup in wheres)
            {
                whereTxt += WherePart(whereGroup, whereTxt);
            }
            if (whereTxt.Length > 1)
            {
                whereTxt = " WHERE " + whereTxt;
            }
            return whereTxt;
        }
        private static string WherePart(WhereGroup whereGroup, string whereTxt)
        {
            whereTxt += "(";
            string subQuery = "";
            IWhereGroup? lastGroup = null;
            foreach (IWhereGroup queryGroup in whereGroup.Groups)
            {
                if (queryGroup is WhereGroup childWhereGroup)
                {
                    subQuery += WherePart(childWhereGroup, "");
                }
                else if (queryGroup is WhereGroupFct fctGroup)
                {
                    subQuery += GetFctName(fctGroup.Fct);
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
            if (whereGroup.negate)
            {
                whereTxt = "!" + whereTxt;
            }
            return whereTxt;
        }

        public static string GetFctName(WhereGroupFctEnum fctEnum)
        {
            return fctEnum switch
            {
                WhereGroupFctEnum.Add => " + ",
                WhereGroupFctEnum.And => " AND ",
                WhereGroupFctEnum.ContainsStr or WhereGroupFctEnum.StartsWith or WhereGroupFctEnum.EndsWith => " LIKE ",
                WhereGroupFctEnum.Divide => " / ",
                WhereGroupFctEnum.Equal => " = ",
                WhereGroupFctEnum.GreaterThan => " > ",
                WhereGroupFctEnum.GreaterThanOrEqual => " >= ",
                WhereGroupFctEnum.LessThan => " < ",
                WhereGroupFctEnum.LessThanOrEqual => " <= ",
                WhereGroupFctEnum.Multiply => " * ",
                WhereGroupFctEnum.Not => " NOT ",
                WhereGroupFctEnum.NotEqual => " <> ",
                WhereGroupFctEnum.Or => " OR ",
                WhereGroupFctEnum.Subtract => " - ",
                WhereGroupFctEnum.ListContains => " IN ",
                _ => "",
            };
        }

    }
}
