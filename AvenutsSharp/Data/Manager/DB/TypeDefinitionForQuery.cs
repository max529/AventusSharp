using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

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
    public class WhereQueryGroupConstantParameter : IWhereQueryGroup
    {
        public string value { get; set; }
        public bool mustBeEscaped { get; set; }
        public WhereQueryGroupConstantParameter(string value, bool mustBeEscaped)
        {
            this.value = value;
            this.mustBeEscaped = mustBeEscaped;
        }
    }
    public class WhereQueryGroupField : IWhereQueryGroup
    {
        public string alias { get; set; }
        public TableMemberInfo tableMemberInfo { get; set; }
    }

    public class ParamsQueryInfo
    {
        public string name { get; set; } = "";
        public Type typeLvl0 { get; set; } = typeof(Object);
        public DbType dbType { get; set; }

        public List<TableMemberInfo> membersList { get; set; } = new List<TableMemberInfo>();

        public object? value { get; set; }

        public bool IsNameSimilar(string name)
        {
            return Regex.IsMatch(name, @"^" + name + @"(\.|$)");
        }
        public void SetValue(object value)
        {
            if (value.GetType() == typeLvl0)
            {
                object? valueToSet = value;
                foreach (TableMemberInfo member in membersList)
                {
                    valueToSet = member.GetValue(valueToSet);
                    if (valueToSet == null)
                    {
                        break;
                    }
                }
                this.value = valueToSet;
            }
        }
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

}
