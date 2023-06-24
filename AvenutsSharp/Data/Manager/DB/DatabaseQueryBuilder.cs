using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB
{
    public class DatabaseQueryBuilder<T> : QueryBuilder<T>
    {
        public IStorage Storage { get; private set; }

        public Dictionary<string, DatabaseQueryBuilderInfo> infoByPath { get; set; } = new Dictionary<string, DatabaseQueryBuilderInfo>();

        public List<string> aliases { get; set; } = new List<string>();
        public Dictionary<Type, TableInfo> loadedTableInfo { get; set; } = new Dictionary<Type, TableInfo>();
        public bool allMembers { get; set; } = true;
        public List<WhereQueryGroup>? wheres { get; set; } = null;

        public bool replaceVarsByParameters { get; set; } = false;

        public Dictionary<string, ParamsQueryInfo> paramsInfo = new Dictionary<string, ParamsQueryInfo>(); // type is the type of the variable to use
        
        public string? sql { get; set; } = null;

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

        public void loadLinks(List<string> pathSplitted, List<Type> types)
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
            ResultWithError<List<T>> result = Storage.QueryFromBuilder(this);
            if (result.Success)
            {
                return result.Result;
            }
            return new List<T>();
        }

        public override QueryBuilder<T> Where(Expression<Func<T, bool>> expression)
        {
            if (wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            replaceVarsByParameters = false;
            LambdaTranslator<T> translator = new LambdaTranslator<T>(this);
            wheres = translator.Translate(expression);

            return this;
        }

        public override QueryBuilder<T> WhereWithParameters(Expression<Func<T, bool>> expression)
        {
            if (wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            replaceVarsByParameters = true;
            LambdaTranslator<T> translator = new LambdaTranslator<T>(this);
            wheres = translator.Translate(expression);

            return this;
        }

        public override QueryBuilder<T> Prepare(params object[] objects)
        {
            List<ParamsQueryInfo> toSet = paramsInfo.Values.ToList();
            foreach(object obj in objects)
            {
                foreach (ParamsQueryInfo info in toSet)
                {
                    if(obj.GetType() == info.typeLvl0)
                    {
                        info.SetValue(obj);
                        toSet.Remove(info);
                        break;
                    }
                }
            }
            if(toSet.Count > 0)
            {
                throw new Exception("Can't found a value to set for variables : " + string.Join(", ", toSet.Select(t => t.name)));
            }
            
            return this;
        }
        public override QueryBuilder<T> SetVariable(string name, object value)
        {
            foreach (KeyValuePair<string, ParamsQueryInfo> paramInfo in paramsInfo)
            {
                if (paramInfo.Value.IsNameSimilar(name))
                {
                    paramInfo.Value.SetValue(value);
                }
            }
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



    }
}
