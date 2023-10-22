using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace AventusSharp.Data.Manager.DB
{
    public enum WhereGroupFctEnum
    {
        None,
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
        ContainsStr,
        ListContains
    }

    public interface IWhereGroup { }
    public class WhereGroup : IWhereGroup
    {
        public List<IWhereGroup> Groups { get; set; } = new List<IWhereGroup>();
        public bool negate = false;

    }
    public class WhereGroupFct : IWhereGroup
    {
        public WhereGroupFctEnum Fct { get; set; }
        public WhereGroupFct(WhereGroupFctEnum fct)
        {
            Fct = fct;
        }
    }
    public class WhereGroupConstantNull : IWhereGroup
    {
    }
    public class WhereGroupConstantString : IWhereGroup
    {
        public string Value { get; set; } = "";
        public WhereGroupConstantString(string value)
        {
            Value = value;
        }
    }
    public class WhereGroupConstantOther : IWhereGroup
    {
        public string Value { get; set; } = "";
        public WhereGroupConstantOther(string value)
        {
            Value = value;
        }
    }
    public class WhereGroupConstantBool : IWhereGroup
    {
        public bool Value { get; set; }
        public WhereGroupConstantBool(bool value)
        {
            Value = value;
        }
    }
    public class WhereGroupConstantDateTime : IWhereGroup
    {
        public DateTime Value { get; set; }
        public WhereGroupConstantDateTime(DateTime value)
        {
            Value = value;
        }
    }
    public class WhereGroupConstantParameter : IWhereGroup
    {
        public string Value { get; set; }
        public WhereGroupConstantParameter(string value)
        {
            Value = value;
        }
    }
    public class WhereGroupField : IWhereGroup
    {
        public string Alias { get; set; }
        public TableMemberInfo TableMemberInfo { get; set; }

        public WhereGroupField(string alias, TableMemberInfo tableMemberInfo)
        {
            Alias = alias;
            TableMemberInfo = tableMemberInfo;
        }
    }

    public class ParamsInfo
    {
        public string Name { get; set; } = "";
        public Type TypeLvl0 { get; set; } = typeof(object);
        public DbType DbType { get; set; }
        public List<TableMemberInfo> MembersList { get; set; } = new List<TableMemberInfo>();

        public object? Value { get; set; }

        public WhereGroupFctEnum FctMethodCall { get; set; }

        public bool IsNameSimilar(string name)
        {
            return Regex.IsMatch(name, @"^" + name + @"(\.|$)");
        }
        public void SetValue(object value)
        {
            if (value.GetType() == TypeLvl0)
            {
                object? valueToSet = value;
                foreach (TableMemberInfo member in MembersList)
                {
                    valueToSet = member.GetValue(valueToSet);
                    if (valueToSet == null)
                    {
                        break;
                    }
                }
                if(valueToSet is IStorable storable)
                {
                    valueToSet = storable.Id;
                }

                this.Value = valueToSet;
            }
        }

        public void SetCurrentValueOnObject(object baseObj)
        {
            Type toCheck = TypeLvl0;
            if (toCheck.IsGenericType)
            {
                try
                {
                    toCheck = toCheck.MakeGenericType(baseObj.GetType());
                }
                catch { }
            }
            if (toCheck.IsInstanceOfType(baseObj))
            {
                object? whereToSet = baseObj;
                for (int i = 0; i < MembersList.Count - 1; i++)
                {
                    whereToSet = MembersList[i].GetValue(whereToSet);
                    if (whereToSet == null)
                    {
                        break;
                    }
                }
                if (whereToSet != null)
                {
                    MembersList[^1].SetValue(whereToSet, Value);
                }
            }
        }

        public List<string> IsValueValid()
        {
            TableMemberInfo memberInfo = MembersList[^1];
            return memberInfo.IsValid(Value);
        }

    }

    public class DatabaseBuilderInfoChild
    {
        public string Alias { get; set; }
        public TableInfo TableInfo { get; set; }

        public List<DatabaseBuilderInfoChild> Children { get; set; } = new List<DatabaseBuilderInfoChild>();

        public DatabaseBuilderInfoChild(string alias, TableInfo tableInfo)
        {
            this.Alias = alias;
            this.TableInfo = tableInfo;
        }
    }
    public class DatabaseBuilderInfoMember
    {
        public TableMemberInfo MemberInfo { get; set; }
        public string Alias { get; set; }
        public bool UseDM { get; set; }

        public Type? Type { get; }

        public TableMemberInfo? TypeMemberInfo { get; }

        public DatabaseBuilderInfoMember(TableMemberInfo memberInfo, string alias, IDBStorage from)
        {
            this.MemberInfo = memberInfo;
            this.Alias = alias;
            Type = memberInfo.Type;
            if (memberInfo.Link == TableMemberInfoLink.Simple)
            {
                if (memberInfo.DM is IDatabaseDM databaseDM && databaseDM.NeedLocalCache && databaseDM.Storage == from)
                {
                    UseDM = true;
                }
                else if (memberInfo.TableInfo.IsAbstract)
                {
                    TypeMemberInfo = memberInfo.TableInfo.Members.Find(m => m.SqlName == TableInfo.TypeIdentifierName);
                }
            }

        }

        public bool IsGeneric()
        {
            if (Type != null)
            {
                if (Type.IsGenericType || Type.IsInterface)
                {
                    return true;
                }
            }
            return false;
        }

    }
    public class DatabaseBuilderInfo
    {
        public TableInfo TableInfo { get; set; }
        public string Alias { get; set; }
        public Dictionary<TableMemberInfo, DatabaseBuilderInfoMember> Members { get; set; } = new Dictionary<TableMemberInfo, DatabaseBuilderInfoMember>();

        public Dictionary<TableInfo, string> Parents { get; set; } = new Dictionary<TableInfo, string>(); // string is the alias
        public List<DatabaseBuilderInfoChild> Children { get; set; } = new List<DatabaseBuilderInfoChild>();

        public Dictionary<TableMemberInfo, DatabaseBuilderInfo> links = new();

        public List<TableMemberInfo> ReverseLinks { get; set; } = new List<TableMemberInfo>();
        
        public DatabaseBuilderInfo(string alias, TableInfo tableInfo)
        {
            this.Alias = alias;
            this.TableInfo = tableInfo;
        }
        public KeyValuePair<TableMemberInfo?, string> GetTableMemberInfoAndAlias(string field)
        {
            string aliasTemp = Alias;
            KeyValuePair<TableMemberInfo?, string> result = new();
            TableMemberInfo? memberInfo = null;
            memberInfo = TableInfo.Members.Find(m => m.Name == field);
            if (memberInfo == null)
            {
                foreach (KeyValuePair<TableInfo, string> parent in Parents)
                {
                    memberInfo = parent.Key.Members.Find(m => m.Name == field);
                    if (memberInfo != null)
                    {
                        aliasTemp = parent.Value;
                        break;
                    }
                }
            }
            result = new KeyValuePair<TableMemberInfo?, string>(memberInfo, aliasTemp);
            return result;
        }

        public TableMemberInfo? GetReverseTableMemberInfo(string field)
        {
            TableMemberInfo? memberInfo = null;
            memberInfo = TableInfo.ReverseMembers.Find(m => m.Name == field);
            if (memberInfo == null)
            {
                foreach (KeyValuePair<TableInfo, string> parent in Parents)
                {
                    memberInfo = parent.Key.Members.Find(m => m.Name == field);
                    if (memberInfo != null)
                    {
                        break;
                    }
                }
            }
            return memberInfo;
        }
    }

}
