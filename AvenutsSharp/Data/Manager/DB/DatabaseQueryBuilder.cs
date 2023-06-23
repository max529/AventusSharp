using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB
{
    public enum WhereQueryGroupFctEnum
    {
        Not,
        And,
        Or,
        Equal,
        NotEqual,
        LessThan,
        GreaterThan,
        LessThanOrEqual,
        GreaterThanOrEqual,
        Add,
        Subtract,
        Multiply,
        Divide,
        StartsWith,
        EndsWith,
        Contains
    }

    public interface IWhereQueryGroup { }
    public class WhereQueryGroup : IWhereQueryGroup
    {
        public List<IWhereQueryGroup> queryGroups { get; set; } = new List<IWhereQueryGroup>();

    }
    public class WhereQueryGroupFct : IWhereQueryGroup
    {
        public WhereQueryGroupFctEnum fct { get; set; }
        public WhereQueryGroupFct(WhereQueryGroupFctEnum fct)
        {
            this.fct = fct;
        }
    }
    public class WhereQueryGroupConstantNull : IWhereQueryGroup
    {
    }
    public class WhereQueryGroupConstantString : IWhereQueryGroup
    {
        public string value { get; set; } = "";
        public WhereQueryGroupConstantString(string value)
        {
            this.value = value;
        }
    }
    public class WhereQueryGroupConstantOther : IWhereQueryGroup
    {
        public string value { get; set; } = "";
        public WhereQueryGroupConstantOther(string value)
        {
            this.value = value;
        }
    }
    public class WhereQueryGroupConstantBool : IWhereQueryGroup
    {
        public bool value { get; set; }
        public WhereQueryGroupConstantBool(bool value)
        {
            this.value = value;
        }
    }
    public class WhereQueryGroupConstantDateTime : IWhereQueryGroup
    {
        public DateTime value { get; set; }
        public WhereQueryGroupConstantDateTime(DateTime value)
        {
            this.value = value;
        }
    }

    public class WhereQueryGroupField : IWhereQueryGroup
    {
        public string alias { get; set; }
        public TableMemberInfo tableMemberInfo { get; set; }
    }

    public class DatabaseQueryBuilderInfoChild
    {
        public string alias { get; set; }
        public TableInfo tableInfo { get; set; }

        public List<DatabaseQueryBuilderInfoChild> children { get; set; } = new List<DatabaseQueryBuilderInfoChild>();
    }
    public class DatabaseQueryBuilderInfo
    {
        public TableInfo tableInfo { get; set; }
        public string alias { get; set; }
        public Dictionary<TableMemberInfo, string> members { get; set; } = new Dictionary<TableMemberInfo, string>();

        public Dictionary<TableInfo, string> parents { get; set; } = new Dictionary<TableInfo, string>(); // string is the alias
        public List<DatabaseQueryBuilderInfoChild> children { get; set; } = new List<DatabaseQueryBuilderInfoChild>();

        public Dictionary<DatabaseQueryBuilderInfo, TableMemberInfo> links = new Dictionary<DatabaseQueryBuilderInfo, TableMemberInfo>();
        public KeyValuePair<TableMemberInfo, string> GetTableMemberInfoAndAlias(string field)
        {
            KeyValuePair<TableMemberInfo, string> result = new KeyValuePair<TableMemberInfo, string>();
            TableMemberInfo? memberInfo = null;
            memberInfo = tableInfo.members.Find(m => m.Name == field);
            if (memberInfo == null)
            {
                foreach (KeyValuePair<TableInfo, string> parent in parents)
                {
                    memberInfo = parent.Key.members.Find(m => m.Name == field);
                    if (memberInfo != null)
                    {
                        return result;
                    }
                }
                return result;
            }
            result = new KeyValuePair<TableMemberInfo, string>(memberInfo, alias);
            return result;
        }
    }


    public class DatabaseQueryBuilder<T> : QueryBuilder<T>
    {
        protected IStorage Storage { get; private set; }

        public Dictionary<string, DatabaseQueryBuilderInfo> infoByPath { get; set; } = new Dictionary<string, DatabaseQueryBuilderInfo>();

        public List<string> aliases { get; set; } = new List<string>();
        public Dictionary<Type, TableInfo> loadedTableInfo { get; set; } = new Dictionary<Type, TableInfo>();
        public bool allMembers { get; set; } = true;
        public List<WhereQueryGroup>? wheres { get; set; } = null;


        public DatabaseQueryBuilder(IStorage storage) : base()
        {
            Storage = storage;
            // load basic info for the main class
            TableInfo tableInfo = GetTableInfo(typeof(T));
            loadTable(tableInfo, "");
        }

        private TableInfo GetTableInfo(Type u)
        {
            if (loadedTableInfo.ContainsKey(u))
            {
                return loadedTableInfo[u];
            }

            TableInfo? tableInfo = Storage.GetTableInfo(u);
            if (tableInfo != null)
            {
                loadedTableInfo.Add(u, tableInfo);
                return tableInfo;
            }
            throw new Exception();
        }
        private string CreateAlias(TableInfo tableInfo)
        {
            string alias = string.Concat(tableInfo.Type.Name.Where(c => char.IsUpper(c)));
            if (alias.Length == 0)
            {
                alias = tableInfo.Type.Name.Substring(0, 2);
            }
            int i = 1;
            string baseAlias = alias;
            while (aliases.Contains(alias))
            {
                alias = baseAlias + i;
                i++;
            }
            aliases.Add(alias);
            return alias;
        }

        private DatabaseQueryBuilderInfo loadTable(TableInfo table, string path)
        {
            if (infoByPath.ContainsKey(path))
            {
                return infoByPath[path];
            }
            string alias = CreateAlias(table);

            DatabaseQueryBuilderInfo info = new DatabaseQueryBuilderInfo()
            {
                alias = alias,
                tableInfo = table
            };
            infoByPath[path] = info;

            loadParent(table, info);
            loadChildren(table, info, info.children);
            return info;
        }
        private void loadParent(TableInfo table, DatabaseQueryBuilderInfo info)
        {
            if (table.Parent != null)
            {
                TableInfo parent = table.Parent;
                string alias = CreateAlias(parent);
                info.parents[parent] = alias;
                loadParent(parent, info);
            }
        }
        private void loadChildren(TableInfo table, DatabaseQueryBuilderInfo info, List<DatabaseQueryBuilderInfoChild> list)
        {
            foreach (TableInfo child in table.Children)
            {
                DatabaseQueryBuilderInfoChild childInfo = new DatabaseQueryBuilderInfoChild()
                {
                    alias = CreateAlias(child),
                    children = new List<DatabaseQueryBuilderInfoChild>(),
                    tableInfo = child,
                };
                list.Add(childInfo);
                loadChildren(child, info, childInfo.children);
            }
        }

        private void loadLinks(List<string> pathSplitted, List<Type> types)
        {
            string currentPath = "";
            DatabaseQueryBuilderInfo parentInfo = infoByPath[currentPath];
            for (int i = 0; i < pathSplitted.Count; i++)
            {

                if (types[i].GetInterfaces().Contains(typeof(IStorable)))
                {
                    if (i > 0)
                    {
                        currentPath += ".";
                    }
                    currentPath += pathSplitted[i];


                    if (!infoByPath.ContainsKey(currentPath))
                    {
                        KeyValuePair<TableMemberInfo, string> memberInfo = parentInfo.GetTableMemberInfoAndAlias(pathSplitted[i]);
                        if (memberInfo.Key != null)
                        {
                            DatabaseQueryBuilderInfo currentTable = loadTable(GetTableInfo(types[i]), currentPath);
                            parentInfo.links[currentTable] = memberInfo.Key;
                        }
                        else
                        {
                            throw new Exception("Can't query " + pathSplitted[i] + " on " + parentInfo.tableInfo.Type.Name);
                        }
                    }
                }
            }
        }




        public override void Execute()
        {
            throw new NotImplementedException();
        }

        public override List<T> Query()
        {
            Storage.BuildQueryFromBuilder(this);
            return new List<T>();
        }

        public override QueryBuilder<T> Where(Expression<Func<T, bool>> expression)
        {
            if (wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            LambdaTranslator translator = new LambdaTranslator(this);
            wheres = translator.Translate(expression);

            return this;
        }

        public override QueryBuilder<T> Field(Expression<Func<T, object>> expression)
        {
            // the strucutre must be Lambda => Convert? => (member x times)
            if (expression is LambdaExpression lambdaExpression)
            {
                Expression exp = lambdaExpression.Body;
                if (lambdaExpression.Body is UnaryExpression convertExpression)
                {
                    exp = convertExpression.Operand;
                }

                if (exp is MemberExpression memberExpression)
                {
                    List<Type> types = new List<Type>();
                    List<string> names = new List<string>();

                    types.Insert(0, memberExpression.Type);
                    names.Insert(0, memberExpression.Member.Name);

                    Expression? temp = memberExpression.Expression;
                    while (temp is MemberExpression temp2)
                    {
                        types.Insert(0, temp2.Type);
                        names.Insert(0, temp2.Member.Name);
                        temp = temp2.Expression;
                        Console.WriteLine("");
                    }

                    loadLinks(names, types);

                    string fullPath = string.Join(".", names.SkipLast(1));
                    allMembers = false;
                    KeyValuePair<TableMemberInfo, string> memberInfo = infoByPath[fullPath].GetTableMemberInfoAndAlias(memberExpression.Member.Name);
                    if (memberInfo.Key != null && !infoByPath[fullPath].members.ContainsKey(memberInfo.Key))
                    {
                        infoByPath[fullPath].members[memberInfo.Key] = memberInfo.Value;
                    }

                    return this;
                }
            }

            throw new Exception();
        }

        public override QueryBuilder<T> Include(Expression<Func<T, IStorable>> expression)
        {
            // the strucutre must be Lambda => Convert? => (member x times)
            if (expression is LambdaExpression lambdaExpression)
            {
                Expression exp = lambdaExpression.Body;
                if (lambdaExpression.Body is UnaryExpression convertExpression)
                {
                    exp = convertExpression.Operand;
                }

                if (exp is MemberExpression memberExpression)
                {
                    List<Type> types = new List<Type>();
                    List<string> names = new List<string>();

                    types.Insert(0, memberExpression.Type);
                    names.Insert(0, memberExpression.Member.Name);

                    Expression? temp = memberExpression.Expression;
                    while (temp is MemberExpression temp2)
                    {
                        types.Insert(0, temp2.Type);
                        names.Insert(0, temp2.Member.Name);
                        temp = temp2.Expression;
                        Console.WriteLine("");
                    }

                    loadLinks(names, types);

                    return this;
                }
            }
            throw new Exception();
        }


        public class LambdaTranslator : ExpressionVisitor
        {
            public List<string> pathes = new List<string>();
            public List<Type> types = new List<Type>();
            private DatabaseQueryBuilder<T> databaseQueryBuilder;

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
                        if (m.Member is FieldInfo fieldInfo)
                        {
                            object? value = fieldInfo.GetValue(container);
                            Visit(Expression.Constant(value));
                        }
                        else if(m.Member is PropertyInfo propertyInfo)
                        {
                            object? value = propertyInfo.GetValue(container);
                            Visit(Expression.Constant(value));
                        }
                        else
                        {
                            Visit(m.Expression);
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
                    if(methodName == "StartsWith")
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
                foreach(Expression argument in node.Arguments)
                {
                    Visit(argument);
                }

                queryGroups.RemoveAt(queryGroups.Count - 1);
                currentGroup = queryGroups.LastOrDefault();
                return node;
            }

            protected bool IsNullConstant(Expression exp)
            {
                return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null);
            }
        }



    }



}
