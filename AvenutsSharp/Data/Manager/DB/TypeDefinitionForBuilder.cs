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
        Contains
    }

    public interface IWhereGroup { }
    public class WhereGroup : IWhereGroup
    {
        public List<IWhereGroup> groups { get; set; } = new List<IWhereGroup>();

    }
    public class WhereGroupFct : IWhereGroup
    {
        public WhereGroupFctEnum fct { get; set; }
        public WhereGroupFct(WhereGroupFctEnum fct)
        {
            this.fct = fct;
        }
    }
    public class WhereGroupConstantNull : IWhereGroup
    {
    }
    public class WhereGroupConstantString : IWhereGroup
    {
        public string value { get; set; } = "";
        public WhereGroupConstantString(string value)
        {
            this.value = value;
        }
    }
    public class WhereGroupConstantOther : IWhereGroup
    {
        public string value { get; set; } = "";
        public WhereGroupConstantOther(string value)
        {
            this.value = value;
        }
    }
    public class WhereGroupConstantBool : IWhereGroup
    {
        public bool value { get; set; }
        public WhereGroupConstantBool(bool value)
        {
            this.value = value;
        }
    }
    public class WhereGroupConstantDateTime : IWhereGroup
    {
        public DateTime value { get; set; }
        public WhereGroupConstantDateTime(DateTime value)
        {
            this.value = value;
        }
    }
    public class WhereGroupConstantParameter : IWhereGroup
    {
        public string value { get; set; }
        public WhereGroupConstantParameter(string value)
        {
            this.value = value;
        }
    }
    public class WhereGroupField : IWhereGroup
    {
        public string alias { get; set; }
        public TableMemberInfo tableMemberInfo { get; set; }
    }

    public class ParamsInfo
    {
        public string name { get; set; } = "";
        public Type typeLvl0 { get; set; } = typeof(object);
        public DbType dbType { get; set; }

        public List<TableMemberInfo> membersList { get; set; } = new List<TableMemberInfo>();

        public object? value { get; set; }

        public WhereGroupFctEnum fctMethodCall { get; set; }

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

    public class DatabaseBuilderInfoChild
    {
        public string alias { get; set; }
        public TableInfo tableInfo { get; set; }

        public List<DatabaseBuilderInfoChild> children { get; set; } = new List<DatabaseBuilderInfoChild>();
    }
    public class DatabaseBuilderInfo
    {
        public TableInfo tableInfo { get; set; }
        public string alias { get; set; }
        public Dictionary<TableMemberInfo, string> members { get; set; } = new Dictionary<TableMemberInfo, string>();

        public Dictionary<TableInfo, string> parents { get; set; } = new Dictionary<TableInfo, string>(); // string is the alias
        public List<DatabaseBuilderInfoChild> children { get; set; } = new List<DatabaseBuilderInfoChild>();

        public Dictionary<TableMemberInfo, DatabaseBuilderInfo> links = new Dictionary<TableMemberInfo, DatabaseBuilderInfo>();
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
