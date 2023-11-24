using AventusSharp.Attributes.Data;
using AventusSharp.Data.Attributes;
using System.Collections.Generic;
using System.Data;
using System;
using System.Reflection;
using System.Linq;
using System.Collections;

namespace AventusSharp.Data.Storage.Default.TableMember
{
    public interface ITableMemberInfoSqlLink
    {
        public TableInfo? TableLinked { get; set; }
        public Type? TableLinkedType { get; }
    }
    public interface ITableMemberInfoSqlWritable
    {
        public DbType SqlType { get; }
    }
    public interface ITableMemberInfoSqlLinkSingle : ITableMemberInfoSqlLink, ITableMemberInfoSqlWritable
    {

    }
    public interface ITableMemberInfoSqlLinkMultiple : ITableMemberInfoSqlLink
    {
        public string? TableIntermediateName { get; }
        public string? TableIntermediateKey1 { get; }
        public string? TableIntermediateKey2 { get; }
        public DbType LinkFieldType { get; }
        public string LinkTableName { get; }

        public string LinkPrimaryName { get; }

    }
    public abstract class TableMemberInfoSql : TableMemberInfo
    {
        public static DbType? GetDbType(Type? type)
        {
            if (type == null)
                return null;
            if (type == typeof(int))
                return DbType.Int32;
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
                return DbType.Double;
            if (type == typeof(string))
                return DbType.String;
            if (type == typeof(bool))
                return DbType.Boolean;
            if (type == typeof(DateTime))
                return DbType.DateTime;
            if (type.IsEnum)
                return DbType.String;
            if (IsTypeUsable(type))
                return DbType.Int32;
            return null;
        }

        public static bool IsTypeUsable(Type type)
        {
            if (type == null)
            {
                return false;
            }
            return type.GetInterfaces().Contains(typeof(IStorable));
        }
        public static Type? IsListTypeUsable(Type type)
        {
            if (type.IsGenericType && type.GetInterfaces().Contains(typeof(IList)))
            {
                Type typeInList = type.GetGenericArguments()[0];
                if (IsTypeUsable(typeInList))
                {
                    return typeInList;
                }
            }
            return null;
        }
        public static Type? IsDictionaryTypeUsable(Type type)
        {
            if (type.IsGenericType && type.GetInterfaces().Contains(typeof(IDictionary)))
            {
                Type typeIndex = type.GetGenericArguments()[0];
                if (typeIndex == typeof(int))
                {
                    Type typeValue = type.GetGenericArguments()[1];
                    if (IsTypeUsable(typeValue))
                    {
                        return typeValue;
                    }
                }
            }
            return null;
        }


        public static TableMemberInfoSql? CreateSql(MemberInfo memberInfo, TableInfo tableInfo)
        {
            TableMemberInfoSql? result = null;
            Type type = GetMemberInfoType(memberInfo);
            bool isNullable = false;
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                isNullable = true;
                type = type.GetGenericArguments()[0];
            }
            bool isList = type.GetInterfaces().Contains(typeof(IList));
            bool isDico = type.GetInterfaces().Contains(typeof(IDictionary));
            if (memberInfo.GetCustomAttribute<ForeignKey>() != null)
            {
                if (isList && type.GetGenericArguments()[0] == typeof(int))
                {
                    result = new TableMemberInfoSqlNMInt(memberInfo, tableInfo, isNullable);
                    return result;
                }
                if (type == typeof(int))
                {
                    return new TableMemberInfoSql1NInt(memberInfo, tableInfo, isNullable);
                }
            }

            if (isList && IsListTypeUsable(type) != null)
            {
                return new TableMemberInfoSqlNM(memberInfo, tableInfo, isNullable);
            }
            else if (isDico && IsDictionaryTypeUsable(type) != null)
            {
                return new TableMemberInfoSqlNM(memberInfo, tableInfo, isNullable);
            }

            else if(IsTypeUsable(type))
            {
                return new TableMemberInfoSql1N(memberInfo, tableInfo, isNullable);
            }

            else if(GetDbType(type) != null)
            {
                return new TableMemberInfoSqlBasic(memberInfo, tableInfo, isNullable);
            }
            return null;
        }
        private static Type GetMemberInfoType(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.FieldType;
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                return propertyInfo.PropertyType;
            }
            throw new Exception("No type for field??");
        }


        public TableMemberInfoSql(TableInfo tableInfo) : base(tableInfo)
        {
        }
        public TableMemberInfoSql(MemberInfo? memberInfo, TableInfo tableInfo, bool isNullable) : base(memberInfo, tableInfo)
        {
            IsNullable = isNullable;
        }

        #region SQL

        public abstract object? GetSqlValue(object obj);
        //public virtual object? GetSqlValue(object obj)
        //{
        //    // TODO maybe check constraint here
        //    if (Link == TableMemberInfoLink.None || Link == TableMemberInfoLink.Parent)
        //    {
        //        return GetValue(obj);
        //    }
        //    else if (Link == TableMemberInfoLink.Simple)
        //    {
        //        object? elementRef = GetValue(obj);
        //        if (elementRef is IStorable storableLink)
        //        {
        //            return storableLink.Id;
        //        }
        //    }
        //    return null;
        //}

        protected abstract void _SetSqlValue(object obj, string value);
        public virtual void SetSqlValue(object? obj, string value)
        {
            if (obj == null)
            {
                return;
            }
            _SetSqlValue(obj, value);
        }
        //public virtual void SetSqlValue(object obj, string value)
        //{
        //    if (obj == null)
        //    {
        //        return;
        //    }
        //    Type? type = Type;
        //    if (type == null)
        //    {
        //        return;
        //    }
        //    if (type == typeof(int))
        //    {
        //        if (int.TryParse(value, out int nb))
        //        {
        //            SetValue(obj, nb);
        //        }
        //    }
        //    else if (type == typeof(double))
        //    {
        //        if (double.TryParse(value, out double nb))
        //        {
        //            SetValue(obj, nb);
        //        }
        //    }
        //    else if (type == typeof(float))
        //    {
        //        if (float.TryParse(value, out float nb))
        //        {
        //            SetValue(obj, nb);
        //        }
        //    }
        //    else if (type == typeof(decimal))
        //    {
        //        if (decimal.TryParse(value, out decimal nb))
        //        {
        //            SetValue(obj, nb);
        //        }
        //    }
        //    else if (type == typeof(string))
        //    {
        //        SetValue(obj, value);
        //    }
        //    else if (type == typeof(bool))
        //    {
        //        if (value == "1")
        //        {
        //            SetValue(obj, true);
        //        }
        //        else
        //        {
        //            SetValue(obj, false);
        //        }
        //    }
        //    else if (type == typeof(DateTime))
        //    {
        //        SetValue(obj, DateTime.Parse(value));
        //    }
        //    else if (type.IsEnum)
        //    {
        //        SetValue(obj, Enum.Parse(type, value.ToString()));
        //    }
        //    else
        //    {
        //        // it's link
        //        if (string.IsNullOrEmpty(value))
        //        {
        //            SetValue(obj, null);
        //        }
        //        else
        //        {
        //            // TODO load reference field
        //            // SetValue(obj, value);
        //        }
        //    }

        //}

        #region link
        //public TableMemberInfoLink Link { get; protected set; } = TableMemberInfoLink.None;
        //public TableInfo? TableLinked { get; set; }
        //public Type? TableLinkedType { get; protected set; }

        //public string? TableIntermediateName
        //{
        //    get
        //    {
        //        if (TableLinked == null)
        //        {
        //            return null;
        //        }
        //        return TableInfo.SqlTableName + "_" + TableLinked.SqlTableName;
        //    }
        //}
        //public string? TableIntermediateKey1
        //{
        //    get
        //    {
        //        return TableInfo.Primary == null ? null : TableInfo.SqlTableName + "_" + TableInfo.Primary.SqlName;
        //    }
        //}

        //public string? TableIntermediateKey2
        //{
        //    get
        //    {
        //        return TableLinked?.Primary == null ? null : TableLinked.SqlTableName + "_" + TableLinked.Primary.SqlName;
        //    }
        //}


        #endregion

        public abstract VoidWithDataError PrepareForSQL();
        //public VoidWithDataError PrepareForSQL()
        //{
        //    if (memberInfo != null)
        //    {
        //        SqlName = memberInfo.Name;
        //    }
        //    return PrepareTypeForSQL();
        //}
        //protected VoidWithDataError PrepareTypeForSQL()
        //{
        //    VoidWithDataError result = new VoidWithDataError();
        //    Type? type = Type;
        //    if (type == null)
        //    {
        //        result.Errors.Add(new DataError(DataErrorCode.FieldTypeNotFound, "Can't found a type for " + SqlName));
        //        return result;
        //    }
        //    if (type == typeof(int))
        //    {
        //        SqlTypeTxt = "int";
        //        SqlType = DbType.Int32;
        //        ForeignKey? attr = GetCustomAttribute<ForeignKey>();
        //        if (attr != null)
        //        {
        //            if (IsTypeUsable(attr.Type))
        //            {
        //                Link = TableMemberInfoLink.SimpleInt;
        //                TableLinkedType = attr.Type;
        //            }
        //            else
        //            {
        //                string errorTxt = "Can't use type " + attr.Type.FullName + " as foreign key inside " + TableInfo.Name;
        //                result.Errors.Add(new DataError(DataErrorCode.TypeNotStorable, errorTxt));
        //                return result;
        //            }
        //        }
        //    }
        //    else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
        //    {
        //        SqlTypeTxt = "float";
        //        SqlType = DbType.Double;
        //    }
        //    else if (type == typeof(string))
        //    {
        //        SqlType = DbType.String;
        //        Size? attr = GetCustomAttribute<Size>();
        //        if (attr != null)
        //        {
        //            if (attr.SizeType == null)
        //                SqlTypeTxt = "varchar(" + attr.Max + ")";
        //            else if (attr.SizeType == SizeEnum.MaxVarChar)
        //                SqlTypeTxt = "varchar(MAX)";
        //            else if (attr.SizeType == SizeEnum.Text)
        //                SqlTypeTxt = "TEXT";
        //            else if (attr.SizeType == SizeEnum.MediumText)
        //                SqlTypeTxt = "MEDIUMTEXT";
        //            else if (attr.SizeType == SizeEnum.LongText)
        //                SqlTypeTxt = "LONGTEXT";
        //        }
        //        else
        //        {
        //            SqlTypeTxt = "varchar(255)";
        //        }
        //    }
        //    else if (type == typeof(bool))
        //    {
        //        SqlType = DbType.Boolean;
        //        SqlTypeTxt = "bit";
        //    }
        //    else if (type == typeof(DateTime))
        //    {
        //        SqlType = DbType.DateTime;
        //        SqlTypeTxt = "datetime";
        //    }
        //    else if (type.IsEnum)
        //    {
        //        SqlType = DbType.String;
        //        SqlTypeTxt = "varchar(255)";
        //    }
        //    else if (IsTypeUsable(type))
        //    {
        //        SqlType = DbType.Int32;
        //        Link = TableMemberInfoLink.Simple;
        //        TableLinkedType = type;
        //        SqlTypeTxt = "int";
        //    }
        //    else
        //    {
        //        // TODO maybe implement both side for N-M link
        //        Type? refType = IsListTypeUsable(type);
        //        if (refType != null)
        //        {
        //            Link = TableMemberInfoLink.Multiple;
        //            TableLinkedType = refType;
        //        }
        //        else
        //        {
        //            refType = IsDictionaryTypeUsable(type);
        //            if (refType != null)
        //            {
        //                Link = TableMemberInfoLink.Multiple;
        //                TableLinkedType = refType;
        //            }
        //        }
        //    }
        //    return result;
        //}

        #endregion

        #region attributes
        public bool IsPrimary { get; protected set; }
        public bool IsAutoIncrement { get; protected set; }
        public bool IsNullable { get; protected set; }
        public bool IsDeleteOnCascade { get; protected set; }
        public bool IsUpdatable { get; internal set; } = true;
        public bool IsUnique { get; internal set; }
        public string SqlName { get; protected set; } = "";


        protected override void ParseAttributes()
        {
            if (!IsNullable)
            {
                IsNullable = DataMainManager.Config?.nullByDefault ?? false;
            }
            base.ParseAttributes();
        }

        protected override bool ParseAttribute(Attribute attribute)
        {
           
            bool result = base.ParseAttribute(attribute);
            if (result)
            {
                return true;
            }
            if (attribute is Primary)
            {
                IsPrimary = true;
                IsUpdatable = false;
                return true;
            }
            if (attribute is AutoIncrement)
            {
                IsAutoIncrement = true;
                return true;
            }
            if (attribute is Attributes.Nullable)
            {
                IsNullable = true;
                return true;
            }
            if (attribute is NotNullable notNullable)
            {
                IsNullable = false;
                return true;
            }
            if (attribute is DeleteOnCascade)
            {
                IsDeleteOnCascade = true;
                return true;
            }
            if (attribute is Unique)
            {
                IsUnique = true;
                return true;
            }
            if (attribute is SqlName attrSqlName)
            {
                SqlName = attrSqlName.Name;
                return true;
            }
            return false;
        }

        #endregion

    }
}
