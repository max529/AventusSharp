using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB
{
    public class DatabaseGenericBuilder<T> : ILambdaTranslatable
    {
        public IStorage Storage { get; private set; }

        public Dictionary<string, DatabaseBuilderInfo> infoByPath { get; set; } = new Dictionary<string, DatabaseBuilderInfo>();

        public Dictionary<string, TableInfo> aliases { get; set; } = new Dictionary<string, TableInfo>();
        public Dictionary<Type, TableInfo> loadedTableInfo { get; set; } = new Dictionary<Type, TableInfo>();
        public List<WhereGroup>? wheres { get; set; } = null;

        public bool replaceWhereByParameters { get; set; } = false;

        public Dictionary<string, ParamsInfo> whereParamsInfo { get; set; } = new Dictionary<string, ParamsInfo>(); // type is the type of the variable to use

        public string? sql { get; set; } = null;


        public DatabaseGenericBuilder(IStorage storage) : base()
        {
            Storage = storage;
            // load basic info for the main class
            TableInfo tableInfo = GetTableInfo(typeof(T));
            loadTable(tableInfo, "");
        }


        protected TableInfo GetTableInfo(Type u)
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
        protected string CreateAlias(TableInfo tableInfo)
        {
            string alias = string.Concat(tableInfo.Type.Name.Where(c => char.IsUpper(c)));
            if (alias.Length == 0)
            {
                alias = tableInfo.Type.Name.Substring(0, 2);
            }
            int i = 1;
            string baseAlias = alias;
            while (aliases.ContainsKey(alias))
            {
                alias = baseAlias + i;
                i++;
            }
            aliases.Add(alias, tableInfo);
            return alias;
        }

        protected DatabaseBuilderInfo loadTable(TableInfo table, string path)
        {
            if (infoByPath.ContainsKey(path))
            {
                return infoByPath[path];
            }
            string alias = CreateAlias(table);

            DatabaseBuilderInfo info = new DatabaseBuilderInfo()
            {
                alias = alias,
                tableInfo = table
            };
            infoByPath[path] = info;

            loadParent(table, info);
            loadChildren(table, info, info.children);
            return info;
        }
        protected void loadParent(TableInfo table, DatabaseBuilderInfo info)
        {
            if (table.Parent != null)
            {
                TableInfo parent = table.Parent;
                string alias = CreateAlias(parent);
                info.parents[parent] = alias;
                loadParent(parent, info);
            }
        }
        protected void loadChildren(TableInfo table, DatabaseBuilderInfo info, List<DatabaseBuilderInfoChild> list)
        {
            foreach (TableInfo child in table.Children)
            {
                DatabaseBuilderInfoChild childInfo = new DatabaseBuilderInfoChild()
                {
                    alias = CreateAlias(child),
                    children = new List<DatabaseBuilderInfoChild>(),
                    tableInfo = child,
                };
                list.Add(childInfo);
                loadChildren(child, info, childInfo.children);
            }
        }

        public void loadLinks(List<string> pathSplitted, List<Type> types, bool addLinksToMembers)
        {
            string currentPath = "";
            DatabaseBuilderInfo parentInfo = infoByPath[currentPath];
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
                            DatabaseBuilderInfo currentTable = loadTable(GetTableInfo(types[i]), currentPath);
                            parentInfo.links[memberInfo.Key] = currentTable;
                            if (addLinksToMembers)
                            {
                                parentInfo.members.Add(memberInfo.Key, memberInfo.Value);
                            }
                        }
                        else
                        {
                            throw new Exception("Can't query " + pathSplitted[i] + " on " + parentInfo.tableInfo.Type.Name);
                        }
                    }
                    else if (addLinksToMembers)
                    {
                        DatabaseBuilderInfo infoTemp = infoByPath[currentPath];
                        KeyValuePair<TableMemberInfo, string> memberInfo = parentInfo.GetTableMemberInfoAndAlias(pathSplitted[i]);
                        if (!parentInfo.members.ContainsKey(memberInfo.Key))
                        {
                            parentInfo.members.Add(memberInfo.Key, memberInfo.Value);
                        }
                    }
                }
            }
        }

        public void _Where(Expression<Func<T, bool>> expression)
        {
            if (wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            replaceWhereByParameters = false;
            LambdaTranslator<T> translator = new LambdaTranslator<T>(this);
            wheres = translator.Translate(expression);
        }
        public void _WhereWithParameters(Expression<Func<T, bool>> expression)
        {
            if (wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            replaceWhereByParameters = true;
            LambdaTranslator<T> translator = new LambdaTranslator<T>(this);
            wheres = translator.Translate(expression);
        }
        public void _Prepare(params object[] objects)
        {
            List<ParamsInfo> toSet = whereParamsInfo.Values.ToList();
            foreach (object obj in objects)
            {
                foreach (ParamsInfo info in toSet)
                {
                    if (obj.GetType() == info.typeLvl0)
                    {
                        info.SetValue(obj);
                        toSet.Remove(info);
                        // set by order first
                        break;
                    }
                }
            }
            if (toSet.Count > 0)
            {
                List<ParamsInfo> toSetClone = toSet.ToList();
                foreach (ParamsInfo info in toSetClone)
                {
                    foreach (object obj in objects)
                    {
                        if (obj.GetType() == info.typeLvl0)
                        {
                            info.SetValue(obj);
                            toSet.Remove(info);
                            // set if same variable used by multiple params
                            break;
                        }
                    }
                }
                if (toSet.Count > 0)
                {
                    throw new Exception("Can't found a value to set for variables : " + string.Join(", ", toSet.Select(t => t.name)));
                }
            }
        }
        public void _SetVariable(string name, object value)
        {
            foreach (KeyValuePair<string, ParamsInfo> paramInfo in whereParamsInfo)
            {
                if (paramInfo.Value.IsNameSimilar(name))
                {
                    paramInfo.Value.SetValue(value);
                }
            }
        }
        public string _Field(Expression<Func<T, object>> expression)
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

                    loadLinks(names, types, true);

                    string fullPath = string.Join(".", names.SkipLast(1));
                    KeyValuePair<TableMemberInfo, string> memberInfo = infoByPath[fullPath].GetTableMemberInfoAndAlias(memberExpression.Member.Name);
                    if (memberInfo.Key != null && !infoByPath[fullPath].members.ContainsKey(memberInfo.Key))
                    {
                        infoByPath[fullPath].members[memberInfo.Key] = memberInfo.Value;
                    }
                    return fullPath != "" ? fullPath+"."+ memberExpression.Member.Name : memberExpression.Member.Name;
                }
            }

            throw new Exception();
        }

        public void _Include(Expression<Func<T, IStorable>> expression)
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

                    loadLinks(names, types, false);
                    return;
                }
            }
            throw new Exception();
        }
    }
}
