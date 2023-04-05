using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Query
{
    internal class WhereQueryInfo
    {
        public string sql { get; set; } = "";
        public List<TableMemberInfo> memberInfos { get; set; } = new List<TableMemberInfo>();
    }
    internal class Where
    {
        private static Dictionary<TableInfo, Dictionary<string, WhereQueryInfo>> queriesInfo = new Dictionary<TableInfo, Dictionary<string, WhereQueryInfo>>();

        private class WhereQueryInfoTemp
        {
            public List<string> fields = new List<string>();
            public List<string> join = new List<string>();
            public List<TableMemberInfo> memberInfos = new List<TableMemberInfo>();

        }

        public static WhereQueryInfo GetQueryInfo(TableInfo tableInfo, BinaryExpression func, MySQLStorage storage)
        {
            string fctTxt = func.ToString();
            if (!queriesInfo.ContainsKey(tableInfo))
            {
                queriesInfo.Add(tableInfo, new Dictionary<string, WhereQueryInfo>());
            }
            if (!queriesInfo[tableInfo].ContainsKey(fctTxt))
            {
                queriesInfo[tableInfo].Add(fctTxt, createQueryInfo(tableInfo, func, storage));
            }
            return queriesInfo[tableInfo][fctTxt];
        }

        private static WhereQueryInfo createQueryInfo(TableInfo table, BinaryExpression func, MySQLStorage storage)
        {
            WhereQueryInfoTemp infoTemp = new WhereQueryInfoTemp();
            GetAllQueryInfo infoGetAll = GetAll.GetQueryInfo(table, storage);
            string whereExp = generateSQLWhere(func, table, storage, infoTemp, infoGetAll.memberInfos);

            WhereQueryInfo infoFinal = new WhereQueryInfo()
            {
                sql = infoGetAll.sql + " WHERE " + whereExp,
                memberInfos = infoGetAll.memberInfos,
            };

            Console.WriteLine(infoFinal.sql);
            return infoFinal;
        }

        private static string generateSQLWhere(Expression e, TableInfo table, MySQLStorage storage, WhereQueryInfoTemp infoTemp, List<TableMemberInfo> memberInfos)
        {
            if (e is BinaryExpression binary)
            {
                if (operators.ContainsKey(e.NodeType))
                {
                    string _operator = operators[e.NodeType];
                    string left = generateSQLWhere(binary.Left, table, storage, infoTemp, memberInfos);
                    string right = generateSQLWhere(binary.Right, table, storage, infoTemp, memberInfos);

                    return left + _operator + right;
                }
                throw new Exception();
            }
            else if (e is MemberExpression member)
            {
                if (member.Expression == null)
                {
                    throw new Exception();
                }

                if (member.Expression is ParameterExpression)
                {

                    TableInfo? tableInfo = storage.GetTableInfo(member.Expression.Type);
                    if (tableInfo == null)
                    {
                        throw new Exception();
                    }
                    if (tableInfo == table)
                    {
                        TableMemberInfo? memberInfo = memberInfos.Find(m => m.Name == member.Member.Name);
                        if (memberInfo == null)
                        {
                            throw new Exception();
                        }
                        return memberInfo.TableInfo.SqlTableName + "." + memberInfo.SqlName;
                    }
                    else
                    {
                        // TODO load dependand table
                        throw new NotImplementedException();
                    }

                }
                else if (member.Expression is ConstantExpression constant)
                {
                    Delegate? f = Expression.Lambda(member).Compile();
                    object? value = f?.DynamicInvoke();
                    if (value != null)
                    {
                        string? valueTxt = value.ToString();
                        if (valueTxt == null)
                        {
                            throw new Exception();
                        }
                        return "'" + valueTxt + "'";
                    }
                }
                else if (member.Expression.NodeType == ExpressionType.Convert)
                {

                    Delegate? f = Expression.Lambda(member.Expression).Compile();
                    object? value = f?.DynamicInvoke();
                    return "";
                }
                else
                {
                    throw new Exception();
                }
            }
            else if (e is ConstantExpression constant)
            {
                if (constant.Value != null)
                {
                    string? valueTxt = constant.Value.ToString();
                    if (valueTxt == null)
                    {
                        throw new Exception();
                    }
                    return "'" + valueTxt + "'";
                }
                throw new Exception();
            }
            throw new Exception();
        }

        private static Dictionary<ExpressionType, string> operators = new Dictionary<ExpressionType, string>() {
            { ExpressionType.Equal, "=" },
            { ExpressionType.And, " AND " },
            { ExpressionType.AndAlso, " AND " },
            { ExpressionType.Or, " OR " },
            { ExpressionType.OrElse, " OR " },
            { ExpressionType.GreaterThan, " > " },
            { ExpressionType.GreaterThanOrEqual, " >= " },
            { ExpressionType.LessThan, " < " },
            { ExpressionType.LessThanOrEqual, " <= " },
        };

    }
}
