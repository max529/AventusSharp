using AventusSharp.Data.Storage.Default;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace AventusSharp.Data.Manager.DB
{
    public interface ILambdaTranslatable
    {
        public bool ReplaceWhereByParameters { get; }
        public IDBStorage Storage { get; }
        public Dictionary<string, ParamsInfo> WhereParamsInfo { get; }
        public Dictionary<string, DatabaseBuilderInfo> InfoByPath { get; }
        public void LoadLinks(List<string> pathSplitted, List<Type> types, bool addLinksToMembers);
    }
    public class LambdaTranslator<T> : ExpressionVisitor
    {
        public List<string> pathes = new();
        public List<Type> types = new();
        private readonly ILambdaTranslatable databaseBuilder;
        private readonly List<DataMemberInfo> variableAccess = new();

        private List<WhereGroup> queryGroups = new();
        private List<WhereGroup> queryGroupsBase = new();
        private WhereGroup? currentGroup;
        private bool onParameter = false;
        private WhereGroupFctEnum fctMethodCall = WhereGroupFctEnum.None;
        private List<Expression?> tree = new List<Expression?>();

        private bool nextGroupNegate = false;
        private Expression? parentExpression
        {
            get
            {
                return tree.Count > 1 ? tree[tree.Count - 2] : null;
            }
        }

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

        [return: NotNullIfNotNull("node")]
        public override Expression? Visit(Expression? node)
        {
            tree.Add(node);
            Expression? result = base.Visit(node);
            tree.RemoveAt(tree.Count - 1);
            return result;
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    if (currentGroup != null)
                        currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.Not));
                    else
                        nextGroupNegate = true;
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

        protected override Expression VisitParameter(ParameterExpression node)
        {
            onParameter = true;
            return base.VisitParameter(node);
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            WhereGroup newGroup = new();
            currentGroup?.Groups.Add(newGroup);
            currentGroup = newGroup;
            if (nextGroupNegate)
            {
                currentGroup.negate = true;
                nextGroupNegate = false;
            }
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
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.And));
                    break;
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.Or));
                    break;
                case ExpressionType.Equal:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.Equal));
                    break;
                case ExpressionType.NotEqual:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.NotEqual));
                    break;
                case ExpressionType.LessThan:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.LessThan));
                    break;
                case ExpressionType.LessThanOrEqual:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.LessThanOrEqual));
                    break;
                case ExpressionType.GreaterThan:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.GreaterThan));
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    currentGroup.Groups.Add(new WhereGroupFct(WhereGroupFctEnum.GreaterThanOrEqual));
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
                currentGroup?.Groups.Add(new WhereGroupConstantNull());
            }
            if (q == null && c.Value != null)
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        currentGroup?.Groups.Add(new WhereGroupConstantBool((bool)c.Value));
                        break;

                    case TypeCode.String:
                        currentGroup?.Groups.Add(new WhereGroupConstantString((string)c.Value));
                        break;

                    case TypeCode.DateTime:
                        currentGroup?.Groups.Add(new WhereGroupConstantDateTime((DateTime)c.Value));
                        break;

                    case TypeCode.Object:
                        if (c.Value.GetType().GetInterfaces().Contains(typeof(IList)))
                        {
                            IList list = (IList)c.Value;
                            List<string?> tupple = new();
                            foreach (object o in list)
                            {
                                tupple.Add(o.ToString());
                            }
                            if (tupple.Count == 0)
                            {
                                tupple.Add("''");
                            }
                            string valueTxt = "(" + string.Join(",", tupple) + ")";
                            currentGroup?.Groups.Add(new WhereGroupConstantOther(valueTxt));
                            break;
                        }
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                    default:
                        string value = c.Value?.ToString() ?? "";
                        currentGroup?.Groups.Add(new WhereGroupConstantOther(value));
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
                                if (databaseBuilder.ReplaceWhereByParameters)
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
                else
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
                                    if (databaseBuilder.ReplaceWhereByParameters)
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
            }
            if (isBase)
            {
                if (onParameter)
                {
                    databaseBuilder.LoadLinks(pathes, types, false);
                    string fullPath = string.Join(".", pathes.SkipLast(1));

                    KeyValuePair<TableMemberInfo?, string> memberInfo = databaseBuilder.InfoByPath[fullPath].GetTableMemberInfoAndAlias(m.Member.Name);
                    if (memberInfo.Key != null)
                    {
                        WhereGroupField field = new(memberInfo.Value, memberInfo.Key);
                        currentGroup?.Groups.Add(field);
                    }
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

            List<Type> listAllowed = new List<Type>()
            {
                typeof(List<int>),
                typeof(List<decimal>),
                typeof(List<double>),
                typeof(List<float>),
                typeof(List<string>),
                typeof(List<bool>),
            };
            string methodName = node.Method.Name;
            Type? onType = node.Object?.Type;
            WhereGroupFctEnum? fct = null;
            bool reverse = false;
            if (onType == typeof(string))
            {
                if (methodName == "StartsWith")
                {
                    fct = WhereGroupFctEnum.StartsWith;
                }
                else if (methodName == "Contains")
                {
                    fct = WhereGroupFctEnum.ContainsStr;
                }
                else if (methodName == "EndsWith")
                {
                    fct = WhereGroupFctEnum.EndsWith;
                }
            }
            else if (onType != null && listAllowed.Contains(onType))
            {
                if (methodName == "Contains")
                {
                    fct = WhereGroupFctEnum.ListContains;
                    reverse = true;
                }
            }

            else if (methodName == "GetElement" && node.Method.DeclaringType == typeof(LambdaExtractVariables))
            {

                ConstantExpression on = transformToConstant(node.Object);
                if (node.Arguments.Count == 1)
                {
                    ConstantExpression param = transformToConstant(node.Arguments[0]);
                    object? result = node.Method.Invoke(on.Value, new object?[] { param.Value });
                    if (parentExpression is BinaryExpression && param.Value != null && result != null)
                    {
                        SetQuickInfoToQueryBuilder(param.Value.ToString() ?? "", result.GetType());
                    }
                    return Expression.Constant(result, node.Method.ReturnType);
                }
            }

            if (fct == null)
            {
                throw new Exception("Method " + methodName + " isn't allowed");
            }

            WhereGroup newGroup = new();
            currentGroup?.Groups.Add(newGroup);
            currentGroup = newGroup;
            if (nextGroupNegate)
            {
                currentGroup.negate = true;
                nextGroupNegate = false;
            }
            if (queryGroups.Count == 0)
            {
                queryGroupsBase.Add(newGroup);
            }
            queryGroups.Add(newGroup);


            fctMethodCall = (WhereGroupFctEnum)fct;
            if (reverse)
            {
                foreach (Expression argument in node.Arguments)
                {
                    Visit(argument);
                }
                currentGroup.Groups.Add(new WhereGroupFct(fctMethodCall));
                Visit(node.Object);
            }
            else
            {
                Visit(node.Object);
                currentGroup.Groups.Add(new WhereGroupFct(fctMethodCall));
                foreach (Expression argument in node.Arguments)
                {
                    Visit(argument);
                }
            }
            fctMethodCall = WhereGroupFctEnum.None;

            queryGroups.RemoveAt(queryGroups.Count - 1);
            currentGroup = queryGroups.LastOrDefault();
            return node;
        }

        private ConstantExpression transformToConstant(Expression? expression)
        {
            if (expression is ConstantExpression constantExpression)
            {
                return constantExpression;
            }

            else if (expression is MemberExpression memberExpression)
            {
                ConstantExpression cst = transformToConstant(memberExpression.Expression);
                DataMemberInfo? member = DataMemberInfo.Create(memberExpression.Member);
                if (member != null && member.Type != null)
                {
                    object? on = member.GetValue(cst.Value);
                    return Expression.Constant(on, member.Type);
                }
            }
            throw new Exception("Can't transform to constant expression");
        }
        private void SetInfoToQueryBuilder()
        {
            List<DataMemberInfo> members = variableAccess;
            List<TableMemberInfo> result = new();

            foreach (DataMemberInfo member in members.ToList())
            {
                if (member.Type != null && member.Type.Name.StartsWith("<>"))
                {
                    members.Remove(member);
                }
            }
            if(members.Count == 0)
            {
                throw new Exception("No member found");
            }

            Type from = members[0].Type ?? throw new Exception("The first members for query " + string.Join(".", members.Select(m => m.Name) + " has no type"));
            string paramName = string.Join(".", variableAccess.Select(v => v.Name));

            if (databaseBuilder.WhereParamsInfo.ContainsKey(paramName))
            {
                // already present
                return;
            }

            Type? lastType = members.Last().Type;
            if (lastType != null && lastType.GetInterfaces().Contains(typeof(IList)))
            {
                lastType = lastType.GetGenericArguments()[0];
            }
            DbType dbType = TableMemberInfo.GetDbType(lastType) ?? throw new Exception("Can't find a type to use inside sql for type " + from.Name);
            TableInfo? tableInfo = databaseBuilder.Storage.GetTableInfo(from);

            for (int i = 1; i < members.Count; i++)
            {
                if (tableInfo == null)
                {
                    throw new Exception("Can't find a table for the type " + from.Name);
                }
                TableMemberInfo memberInfo = tableInfo.Members.Find(m => m.Name == members[i].Name) ?? throw new Exception("Can't find a sql field for the field " + members[i].Name + " on the type " + from.Name);
                result.Add(memberInfo);
                tableInfo = memberInfo.TableLinked;
            }

            databaseBuilder.WhereParamsInfo.Add(paramName, new ParamsInfo()
            {
                DbType = (DbType)dbType,
                MembersList = result,
                Name = paramName,
                TypeLvl0 = from,
                Value = null,
                FctMethodCall = fctMethodCall,
            });

            currentGroup?.Groups.Add(new WhereGroupConstantParameter(paramName));

        }

        private void SetQuickInfoToQueryBuilder(string paramName, Type type)
        {
            if (databaseBuilder.WhereParamsInfo.ContainsKey(paramName))
            {
                // already present
                return;
            }

            DbType dbType = TableMemberInfo.GetDbType(type) ?? throw new Exception("Can't find a type to use inside sql for type " + type.Name);

            databaseBuilder.WhereParamsInfo.Add(paramName, new ParamsInfo()
            {
                DbType = (DbType)dbType,
                MembersList = new List<TableMemberInfo>(),
                Name = paramName,
                TypeLvl0 = type,
                Value = null,
                FctMethodCall = fctMethodCall,
            });

            currentGroup?.Groups.Add(new WhereGroupConstantParameter(paramName));

        }
    }


    public class LambdaToPath : ExpressionVisitor
    {
        private readonly List<string> Pathes = new();

        private static LambdaToPath? instance;
        private static readonly Mutex Mutex = new();


        public static string Translate(Expression expression)
        {
            Mutex.WaitOne();
            instance ??= new LambdaToPath();
            instance.Pathes.Clear();
            instance.Visit(expression);
            string result = string.Join(".", instance.Pathes);
            Mutex.ReleaseMutex();
            return result;
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            Pathes.Insert(0, m.Member.Name);
            return base.VisitMember(m);
        }
    }

    public class LambdaExtractVariables : ExpressionVisitor
    {
        private List<Type> types = new();

        private Dictionary<string, object?> parameters;
        private bool isOnParam = false;
        private int changedLvl = 0;
        private DataMemberInfo? memberInfo;
        private MethodInfo GetElementFct;

        public LambdaExtractVariables(Dictionary<string, object?> parameters)
        {
            this.parameters = parameters;
            GetElementFct = this.GetType().GetMethod("GetElement") ?? throw new Exception("Impossible");
        }
        public Expression Extract(Expression exp)
        {
            return Visit(exp);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (memberInfo != null && !isOnParam)
            {
                changedLvl = 1;
                if (!parameters.ContainsKey(memberInfo.Name))
                {
                    object? o = memberInfo.GetValue(node.Value);
                    if (o != null)
                    {
                        object? newObjTemp = Activator.CreateInstance(o.GetType());
                        parameters.Add(memberInfo.Name, newObjTemp);
                    }
                    else
                    {
                        throw new Exception("You must set a value inside the field" + memberInfo.Name);
                    }
                }
                ConstantExpression constant = Expression.Constant(this, typeof(LambdaExtractVariables));
                return constant;
            }
            return base.VisitConstant(node);
        }
        protected override Expression VisitParameter(ParameterExpression node)
        {
            isOnParam = true;
            return base.VisitParameter(node);
        }
        public X GetElement<X>(string name)
        {
            if (this.parameters.ContainsKey(name) && parameters[name] is X casted)
            {
                return casted;
            }
            throw new Exception("Can't find variable " + name);
        }

        protected override Expression VisitMember(MemberExpression m)
        {
            bool isBase = types.Count == 0;
            if (isBase)
            {
                isOnParam = false;
                changedLvl = 0;
            }
            types.Insert(0, m.Type);

            if (m.Expression != null)
            {
                DataMemberInfo? localMember = DataMemberInfo.Create(m.Member);
                memberInfo = localMember;
                Expression result = Visit(m.Expression);
                if (localMember != null)
                {
                    if (changedLvl == 1 && localMember.Type != null)
                    {
                        // remove first level to remove local variable call
                        MethodInfo method = GetElementFct.MakeGenericMethod(localMember.Type);
                        result = Expression.Call(result, method, Expression.Constant(localMember.Name));
                        changedLvl++;
                        return result;
                    }
                    else if (changedLvl == 2)
                    {
                        m = Expression.Property(result, localMember.Name);
                    }
                }
                memberInfo = null;
            }
            if (isBase)
            {
                types.Clear();
            }
            return m;

        }

    }
}
