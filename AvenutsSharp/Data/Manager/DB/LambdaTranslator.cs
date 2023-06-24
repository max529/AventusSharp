using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AventusSharp.Data.Manager.DB
{
    public class LambdaTranslator<T> : ExpressionVisitor
    {
        public List<string> pathes = new List<string>();
        public List<Type> types = new List<Type>();
        private DatabaseQueryBuilder<T> databaseQueryBuilder;
        private List<DataMemberInfo> variableAccess = new List<DataMemberInfo>();

        private List<WhereQueryGroup> queryGroups = new List<WhereQueryGroup>();
        private List<WhereQueryGroup> queryGroupsBase = new List<WhereQueryGroup>();
        private WhereQueryGroup? currentGroup;
        private bool onParameter = false;

        public LambdaTranslator(DatabaseQueryBuilder<T> databaseQueryBuilder)
        {
            this.databaseQueryBuilder = databaseQueryBuilder;
        }

        public List<WhereQueryGroup> Translate(Expression expression)
        {
            queryGroups = new List<WhereQueryGroup>();
            queryGroupsBase = new List<WhereQueryGroup>();
            Visit(expression);

            return queryGroupsBase;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    currentGroup?.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.Not));
                    Visit(u.Operand);
                    break;
                case ExpressionType.Convert:
                    Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }

            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            WhereQueryGroup newGroup = new WhereQueryGroup();
            if (currentGroup != null)
            {
                currentGroup.queryGroups.Add(newGroup);
            }
            currentGroup = newGroup;
            if (queryGroups.Count == 0)
            {
                queryGroupsBase.Add(newGroup);
            }
            queryGroups.Add(newGroup);

            Visit(b.Left);

            switch (b.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.And));
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.Or));
                    break;
                case ExpressionType.Equal:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.Equal));
                    break;
                case ExpressionType.NotEqual:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.NotEqual));
                    break;
                case ExpressionType.LessThan:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.LessThan));
                    break;
                case ExpressionType.LessThanOrEqual:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.LessThanOrEqual));
                    break;
                case ExpressionType.GreaterThan:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.GreaterThan));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    currentGroup.queryGroups.Add(new WhereQueryGroupFct(WhereQueryGroupFctEnum.GreaterThanOrEqual));
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
            }

            Visit(b.Right);

            queryGroups.RemoveAt(queryGroups.Count - 1);
            currentGroup = queryGroups.LastOrDefault();

            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            IQueryable? q = c.Value as IQueryable;
            if (q == null && c.Value == null)
            {
                currentGroup?.queryGroups.Add(new WhereQueryGroupConstantNull());
            }
            if (q == null && c.Value != null)
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        currentGroup?.queryGroups.Add(new WhereQueryGroupConstantBool((bool)c.Value));
                        break;

                    case TypeCode.String:
                        currentGroup?.queryGroups.Add(new WhereQueryGroupConstantString((string)c.Value));
                        break;

                    case TypeCode.DateTime:
                        currentGroup?.queryGroups.Add(new WhereQueryGroupConstantDateTime((DateTime)c.Value));
                        break;

                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                    default:
                        currentGroup?.queryGroups.Add(new WhereQueryGroupConstantOther((string)c.Value));
                        break;
                }
            }

            return c;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            bool isBase = types.Count == 0;
            if (isBase)
            {
                onParameter = false;
            }

            pathes.Insert(0, m.Member.Name);
            types.Insert(0, m.Type);

            if (m.Expression != null)
            {
                if (m.Expression is ParameterExpression)
                {
                    onParameter = true;
                }
                if (m.Expression is ConstantExpression cst)
                {
                    object? container = cst.Value;
                    DataMemberInfo? memberInfo;
                    if ((memberInfo = DataMemberInfo.Create(m.Member)) != null)
                    {
                        if (container != null)
                        {
                            object? value = memberInfo.GetValue(container);
                            if (isBase)
                            {
                                if (databaseQueryBuilder.replaceVarsByParameters)
                                {
                                    variableAccess.Add(memberInfo);
                                    SetInfoToQueryBuilder();
                                }
                                else
                                {
                                    Visit(Expression.Constant(value));
                                }
                            }
                            else
                            {
                                variableAccess.Add(memberInfo);
                                return Expression.Constant(value);
                            }
                        }
                        else
                        {
                            throw new Exception("Can't parse the full object because of null value found.");
                        }
                    }
                    else
                    {
                        Visit(m.Expression);
                    }
                }
                else if (m.Expression is MemberExpression)
                {
                    Expression result = Visit(m.Expression);
                    if (result is ConstantExpression cstExpression)
                    {
                        DataMemberInfo? memberInfo;
                        if ((memberInfo = DataMemberInfo.Create(m.Member)) != null)
                        {
                            if (cstExpression.Value != null)
                            {
                                object? value = memberInfo.GetValue(cstExpression.Value);
                                if (isBase)
                                {
                                    if (databaseQueryBuilder.replaceVarsByParameters)
                                    {
                                        variableAccess.Add(memberInfo);
                                        SetInfoToQueryBuilder();
                                    }
                                    else
                                    {
                                        Visit(Expression.Constant(value));
                                    }
                                }
                                else
                                {
                                    variableAccess.Add(memberInfo);
                                    return Expression.Constant(value);
                                }
                            }
                            else
                            {
                                throw new Exception("Can't parse the full object because of null value found.");
                            }
                        }
                    }
                }
                else
                {
                    Visit(m.Expression);
                }
            }
            if (isBase)
            {
                if (onParameter)
                {
                    databaseQueryBuilder.loadLinks(pathes, types);
                    string fullPath = string.Join(".", pathes.SkipLast(1));

                    KeyValuePair<TableMemberInfo, string> memberInfo = databaseQueryBuilder.infoByPath[fullPath].GetTableMemberInfoAndAlias(m.Member.Name);

                    WhereQueryGroupField field = new WhereQueryGroupField()
                    {
                        alias = memberInfo.Value,
                        tableMemberInfo = memberInfo.Key
                    };
                    currentGroup?.queryGroups.Add(field);
                }
                pathes.Clear();
                types.Clear();
                variableAccess.Clear();
            }
            return m;

            throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            string methodName = node.Method.Name;
            Type? onType = node.Object?.Type;
            WhereQueryGroupFctEnum? fct = null;

            if (onType == typeof(string))
            {
                if (methodName == "StartsWith")
                {
                    fct = WhereQueryGroupFctEnum.StartsWith;
                }
                else if (methodName == "Contains")
                {
                    fct = WhereQueryGroupFctEnum.Contains;
                }
                else if (methodName == "EndsWith")
                {
                    fct = WhereQueryGroupFctEnum.EndsWith;
                }
            }

            if (fct == null)
            {
                throw new Exception("Method " + methodName + " isn't allowed");
            }

            WhereQueryGroup newGroup = new WhereQueryGroup();
            if (currentGroup != null)
            {
                currentGroup.queryGroups.Add(newGroup);
            }
            currentGroup = newGroup;
            if (queryGroups.Count == 0)
            {
                queryGroupsBase.Add(newGroup);
            }
            queryGroups.Add(newGroup);

            Visit(node.Object);
            currentGroup.queryGroups.Add(new WhereQueryGroupFct((WhereQueryGroupFctEnum)fct));
            foreach (Expression argument in node.Arguments)
            {
                Visit(argument);
            }

            queryGroups.RemoveAt(queryGroups.Count - 1);
            currentGroup = queryGroups.LastOrDefault();
            return node;
        }


        private void SetInfoToQueryBuilder()
        {
            List<DataMemberInfo> members = variableAccess;
            string paramName = string.Join(".", variableAccess.Select(v => v.Name));
            if (databaseQueryBuilder.paramsInfo.ContainsKey(paramName))
            {
                // already present
                return;
            }

            List<TableMemberInfo> result = new List<TableMemberInfo>();

            Type? from = members[0].Type;
            if (from == null)
            {
                throw new Exception("The first members for query " + string.Join(".", members.Select(m => m.Name) + " has no type"));
            }
            DbType? dbType = TableMemberInfo.GetDbType(members.Last().Type);
            if (dbType == null)
            {
                throw new Exception("Can't find a type to use inside sql for type " + from.Name);
            }
            TableInfo? tableInfo = databaseQueryBuilder.Storage.GetTableInfo(from);

            for (int i = 1; i < members.Count; i++)
            {
                if (tableInfo == null)
                {
                    throw new Exception("Can't find a table for the type " + from.Name);
                }
                TableMemberInfo? memberInfo = tableInfo.members.Find(m => m.Name == members[i].Name);
                if (memberInfo == null)
                {
                    throw new Exception("Can't find a sql field for the field " + members[i].Name + " on the type " + from.Name);
                }
                result.Add(memberInfo);
                tableInfo = memberInfo.TableLinked;
            }

            databaseQueryBuilder.paramsInfo.Add(paramName, new ParamsQueryInfo()
            {
                dbType = (DbType)dbType,
                membersList = result,
                name = paramName,
                typeLvl0 = from,
                value = null
            });

            List<DbType> escapedTypes = new List<DbType>() { DbType.String, DbType.DateTime };
            bool mustBeEscaped = escapedTypes.Contains((DbType)dbType);
            currentGroup?.queryGroups.Add(new WhereQueryGroupConstantParameter(paramName, mustBeEscaped));

        }
    }
}
