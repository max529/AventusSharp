using AventusSharp.Data.Attributes;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data.Manager.DB
{
    public class DatabaseGenericBuilder<T> : ILambdaTranslatable
    {
        public IDBStorage Storage { get; private set; }

        public Dictionary<string, DatabaseBuilderInfo> InfoByPath { get; set; } = new Dictionary<string, DatabaseBuilderInfo>();

        public Dictionary<string, TableInfo> Aliases { get; set; } = new Dictionary<string, TableInfo>();
        public Dictionary<Type, TableInfo> LoadedTableInfo { get; set; } = new Dictionary<Type, TableInfo>();
        public List<WhereGroup>? Wheres { get; set; } = null;

        public bool ReplaceWhereByParameters { get; set; } = false;

        public Dictionary<string, ParamsInfo> WhereParamsInfo { get; set; } = new Dictionary<string, ParamsInfo>(); // type is the type of the variable to use


        public string? Sql { get; set; } = null;


        public DatabaseGenericBuilder(IDBStorage storage, Type? baseType = null) : base()
        {
            Storage = storage;
            // load basic info for the main class
            if (baseType == null)
            {
                baseType = typeof(T);
            }
            TableInfo tableInfo = GetTableInfo(baseType);
            LoadTable(tableInfo, "");
        }


        protected TableInfo GetTableInfo(Type u)
        {
            if (LoadedTableInfo.ContainsKey(u))
            {
                return LoadedTableInfo[u];
            }

            TableInfo? tableInfo = Storage.GetTableInfo(u);
            if (tableInfo != null)
            {
                LoadedTableInfo.Add(u, tableInfo);
                return tableInfo;
            }
            throw new Exception();
        }
        protected string CreateAlias(TableInfo tableInfo)
        {
            string alias = string.Concat(tableInfo.Type.Name.Where(c => char.IsUpper(c)));
            if (alias.Length == 0)
            {
                alias = tableInfo.Type.Name[..2];
            }
            int i = 1;
            string baseAlias = alias;
            while (Aliases.ContainsKey(alias))
            {
                alias = baseAlias + i;
                i++;
            }
            Aliases.Add(alias, tableInfo);
            return alias;
        }

        protected DatabaseBuilderInfo LoadTable(TableInfo table, string path)
        {
            if (InfoByPath.ContainsKey(path))
            {
                return InfoByPath[path];
            }
            string alias = CreateAlias(table);

            DatabaseBuilderInfo info = new(alias, table);
            InfoByPath[path] = info;



            LoadParent(table, info);
            LoadChildren(table, info, info.Children);
            return info;
        }
        protected void LoadParent(TableInfo table, DatabaseBuilderInfo info)
        {
            if (table.Parent != null)
            {
                TableInfo parent = table.Parent;
                string alias = CreateAlias(parent);
                info.Parents[parent] = alias;
                LoadParent(parent, info);
            }
        }
        protected void LoadChildren(TableInfo table, DatabaseBuilderInfo info, List<DatabaseBuilderInfoChild> list)
        {
            foreach (TableInfo child in table.Children)
            {
                DatabaseBuilderInfoChild childInfo = new(CreateAlias(child), child);
                list.Add(childInfo);
                LoadChildren(child, info, childInfo.Children);
            }
        }

        public void LoadLinks(List<string> pathSplitted, List<Type> types, bool addLinksToMembers)
        {
            string currentPath = "";
            for (int i = 0; i < pathSplitted.Count; i++)
            {
                DatabaseBuilderInfo parentInfo = InfoByPath[currentPath];
                if (types[i].GetInterfaces().Contains(typeof(IStorable)))
                {
                    if (i > 0)
                    {
                        currentPath += ".";
                    }
                    currentPath += pathSplitted[i];


                    if (!InfoByPath.ContainsKey(currentPath))
                    {
                        KeyValuePair<TableMemberInfo?, string> memberInfo = parentInfo.GetTableMemberInfoAndAlias(pathSplitted[i]);
                        if (memberInfo.Key != null)
                        {

                            DatabaseBuilderInfo currentTable = LoadTable(GetTableInfo(types[i]), currentPath);
                            parentInfo.links[memberInfo.Key] = currentTable;
                            if (addLinksToMembers)
                            {
                                parentInfo.Members.Add(memberInfo.Key, new DatabaseBuilderInfoMember(memberInfo.Key, memberInfo.Value, Storage));
                            }
                        }
                        else
                        {
                            throw new Exception("Can't query " + pathSplitted[i] + " on " + parentInfo.TableInfo.Type.Name);
                        }
                    }
                    else if (addLinksToMembers)
                    {
                        KeyValuePair<TableMemberInfo?, string> memberInfo = parentInfo.GetTableMemberInfoAndAlias(pathSplitted[i]);
                        if (memberInfo.Key != null && !parentInfo.Members.ContainsKey(memberInfo.Key))
                        {
                            parentInfo.Members.Add(memberInfo.Key, new DatabaseBuilderInfoMember(memberInfo.Key, memberInfo.Value, Storage));
                        }
                    }
                }
            }
        }

        protected void WhereGeneric(Expression<Func<T, bool>> expression)
        {
            if (Wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            ReplaceWhereByParameters = false;
            LambdaTranslator<T> translator = new(this);
            Wheres = translator.Translate(expression);
        }
        protected void WhereGenericWithParameters(Expression<Func<T, bool>> expression)
        {
            if (Wheres != null)
            {
                throw new Exception("Can't use twice the where action");
            }
            ReplaceWhereByParameters = true;
            LambdaTranslator<T> translator = new(this);
            Wheres = translator.Translate(expression);
        }
        protected void PrepareGeneric(params object[] objects)
        {
            List<ParamsInfo> toSet = WhereParamsInfo.Values.ToList();
            foreach (object obj in objects)
            {
                foreach (ParamsInfo info in toSet)
                {
                    if (obj.GetType() == info.TypeLvl0)
                    {
                        info.SetValue(obj);
                        OnVariableSet(info, obj);
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
                        if (obj.GetType() == info.TypeLvl0)
                        {
                            info.SetValue(obj);
                            OnVariableSet(info, obj);
                            toSet.Remove(info);
                            // set if same variable used by multiple params
                            break;
                        }
                    }
                }
                if (toSet.Count > 0)
                {
                    throw new Exception("Can't found a value to set for variables : " + string.Join(", ", toSet.Select(t => t.Name)));
                }
            }
        }

        protected virtual void OnVariableSet(ParamsInfo param, object fromObject)
        {

        }
        protected void SetVariableGeneric(string name, object value)
        {
            foreach (KeyValuePair<string, ParamsInfo> paramInfo in WhereParamsInfo)
            {
                if (paramInfo.Value.IsNameSimilar(name))
                {
                    paramInfo.Value.SetValue(value);
                    OnVariableSet(paramInfo.Value, value);
                }
            }
        }
        protected string FieldGeneric(Expression<Func<T, object>> expression)
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
                    List<Type> types = new();
                    List<string> names = new();

                    types.Insert(0, memberExpression.Type);
                    names.Insert(0, memberExpression.Member.Name);

                    Expression? temp = memberExpression.Expression;
                    while (temp is MemberExpression temp2)
                    {
                        types.Insert(0, temp2.Type);
                        names.Insert(0, temp2.Member.Name);
                        temp = temp2.Expression;
                    }

                    LoadLinks(names, types, true);

                    string fullPath = string.Join(".", names.SkipLast(1));
                    KeyValuePair<TableMemberInfo?, string> memberInfo = InfoByPath[fullPath].GetTableMemberInfoAndAlias(memberExpression.Member.Name);
                    if (memberInfo.Key != null)
                    {
                        if (!InfoByPath[fullPath].Members.ContainsKey(memberInfo.Key))
                        {
                            InfoByPath[fullPath].Members[memberInfo.Key] = new DatabaseBuilderInfoMember(memberInfo.Key, memberInfo.Value, Storage);
                        }
                    }
                    else
                    {
                        // if we can't find the members info maybe it's a reverse link
                        TableMemberInfo? reversMemberInfo = InfoByPath[fullPath].GetReverseTableMemberInfo(memberExpression.Member.Name);
                        if (reversMemberInfo != null && !InfoByPath[fullPath].ReverseLinks.Contains(reversMemberInfo))
                        {
                            InfoByPath[fullPath].ReverseLinks.Add(reversMemberInfo);
                        }
                    }
                    return fullPath != "" ? fullPath + "." + memberExpression.Member.Name : memberExpression.Member.Name;
                }
            }

            throw new Exception();
        }

        protected void IncludeGeneric(Expression<Func<T, IStorable>> expression)
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
                    List<Type> types = new();
                    List<string> names = new();

                    types.Insert(0, memberExpression.Type);
                    names.Insert(0, memberExpression.Member.Name);

                    Expression? temp = memberExpression.Expression;
                    while (temp is MemberExpression temp2)
                    {
                        types.Insert(0, temp2.Type);
                        names.Insert(0, temp2.Member.Name);
                        temp = temp2.Expression;
                    }

                    LoadLinks(names, types, false);
                    return;
                }
            }
            throw new Exception();
        }
    }
}
