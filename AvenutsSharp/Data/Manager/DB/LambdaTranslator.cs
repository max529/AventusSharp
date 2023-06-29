using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AventusSharp.Data.Manager.DB
{
    public interface ILambdaTranslatable
    {
        public bool replaceWhereByParameters { get; }
        public IStorage Storage { get; }
        public Dictionary<string, ParamsInfo> whereParamsInfo { get; }
        public Dictionary<string, DatabaseBuilderInfo> infoByPath { get; }
        public void loadLinks(List<string> pathSplitted, List<Type> types, bool addLinksToMembers);
    }
    public class LambdaTranslator<T> : ExpressionVisitor
    {
        public List<string> pathes = new List<string>();
        public List<Type> types = new List<Type>();
        private ILambdaTranslatable databaseBuilder;
        private List<DataMemberInfo> variableAccess = new List<DataMemberInfo>();

        private List<WhereGroup> queryGroups = new List<WhereGroup>();
        private List<WhereGroup> queryGroupsBase = new List<WhereGroup>();
        private WhereGroup? currentGroup;
        private bool onParameter = false;
        private WhereGroupFctEnum fctMethodCall = WhereGroupFctEnum.None;

        public LambdaTranslator(ILambdaTranslatable databaseQueryBuilder)
        {
            this.databaseBuilder = databaseQueryBuilder;
        }

        public List<WhereGroup> Translate(Expression expression)
        {
            queryGroups = new List<WhereGroup>();
            queryGroupsBase = new List<WhereGroup>();
            Visit(expression);

            return queryGroupsBase;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    currentGroup?.groups.Add(new WhereGroupFct(WhereGroupFctEnum.Not));
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
            WhereGroup newGroup = new WhereGroup();
            if (currentGroup != null)
            {
                currentGroup.groups.Add(newGroup);
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
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.And));
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.Or));
                    break;
                case ExpressionType.Equal:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.Equal));
                    break;
                case ExpressionType.NotEqual:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.NotEqual));
                    break;
                case ExpressionType.LessThan:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.LessThan));
                    break;
                case ExpressionType.LessThanOrEqual:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.LessThanOrEqual));
                    break;
                case ExpressionType.GreaterThan:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.GreaterThan));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    currentGroup.groups.Add(new WhereGroupFct(WhereGroupFctEnum.GreaterThanOrEqual));
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
                currentGroup?.groups.Add(new WhereGroupConstantNull());
            }
            if (q == null && c.Value != null)
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        currentGroup?.groups.Add(new WhereGroupConstantBool((bool)c.Value));
                        break;

                    case TypeCode.String:
                        currentGroup?.groups.Add(new WhereGroupConstantString((string)c.Value));
                        break;

                    case TypeCode.DateTime:
                        currentGroup?.groups.Add(new WhereGroupConstantDateTime((DateTime)c.Value));
                        break;

                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                    default:
                        string value = c.Value?.ToString() ?? "";
                        currentGroup?.groups.Add(new WhereGroupConstantOther(value));
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
                                if (databaseBuilder.replaceWhereByParameters)
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
                                    if (databaseBuilder.replaceWhereByParameters)
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
                    databaseBuilder.loadLinks(pathes, types, false);
                    string fullPath = string.Join(".", pathes.SkipLast(1));

                    KeyValuePair<TableMemberInfo, string> memberInfo = databaseBuilder.infoByPath[fullPath].GetTableMemberInfoAndAlias(m.Member.Name);

                    WhereGroupField field = new WhereGroupField()
                    {
                        alias = memberInfo.Value,
                        tableMemberInfo = memberInfo.Key
                    };
                    currentGroup?.groups.Add(field);
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
            WhereGroupFctEnum? fct = null;

            if (onType == typeof(string))
            {
                if (methodName == "StartsWith")
                {
                    fct = WhereGroupFctEnum.StartsWith;
                }
                else if (methodName == "Contains")
                {
                    fct = WhereGroupFctEnum.Contains;
                }
                else if (methodName == "EndsWith")
                {
                    fct = WhereGroupFctEnum.EndsWith;
                }
            }

            if (fct == null)
            {
                throw new Exception("Method " + methodName + " isn't allowed");
            }

            WhereGroup newGroup = new WhereGroup();
            if (currentGroup != null)
            {
                currentGroup.groups.Add(newGroup);
            }
            currentGroup = newGroup;
            if (queryGroups.Count == 0)
            {
                queryGroupsBase.Add(newGroup);
            }
            queryGroups.Add(newGroup);

            fctMethodCall = (WhereGroupFctEnum)fct;
            Visit(node.Object);
            currentGroup.groups.Add(new WhereGroupFct(fctMethodCall));
            foreach (Expression argument in node.Arguments)
            {
                Visit(argument);
            }
            fctMethodCall = WhereGroupFctEnum.None;

            queryGroups.RemoveAt(queryGroups.Count - 1);
            currentGroup = queryGroups.LastOrDefault();
            return node;
        }


        private void SetInfoToQueryBuilder()
        {
            List<DataMemberInfo> members = variableAccess;
            List<TableMemberInfo> result = new List<TableMemberInfo>();

            Type? from = members[0].Type;
            if (from == null)
            {
                throw new Exception("The first members for query " + string.Join(".", members.Select(m => m.Name) + " has no type"));
            }
            string paramName = string.Join(".", variableAccess.Select(v => v.Name));

            if (databaseBuilder.whereParamsInfo.ContainsKey(paramName))
            {
                // already present
                return;
            }

            DbType? dbType = TableMemberInfo.GetDbType(members.Last().Type);
            if (dbType == null)
            {
                throw new Exception("Can't find a type to use inside sql for type " + from.Name);
            }
            TableInfo? tableInfo = databaseBuilder.Storage.GetTableInfo(from);

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

            databaseBuilder.whereParamsInfo.Add(paramName, new ParamsInfo()
            {
                dbType = (DbType)dbType,
                membersList = result,
                name = paramName,
                typeLvl0 = from,
                value = null,
                fctMethodCall = fctMethodCall
            });

            currentGroup?.groups.Add(new WhereGroupConstantParameter(paramName));

        }
    }
}
