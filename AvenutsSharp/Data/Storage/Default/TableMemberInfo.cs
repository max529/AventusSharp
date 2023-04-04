using AventusSharp.Attributes;
using AventusSharp.Tools;
using AvenutsSharp.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Data;

namespace AventusSharp.Data.Storage.Default
{
    public enum TableMemberInfoLink
    {
        None,
        Simple,
        Parent,
        Multiple,
    }
    public class TableMemberInfo
    {
        protected MemberInfo? memberInfo;
        protected Dictionary<Type, MemberInfo> memberInfoByType = new Dictionary<Type, MemberInfo>();
        public readonly TableInfo TableInfo;
        public TableMemberInfo(TableInfo tableInfo)
        {
            TableInfo = tableInfo;
        }
        public TableMemberInfo(FieldInfo fieldInfo, TableInfo tableInfo)
        {
            memberInfo = fieldInfo;
            TableInfo = tableInfo;
        }
        public TableMemberInfo(PropertyInfo propertyInfo, TableInfo tableInfo)
        {
            memberInfo = propertyInfo;
            TableInfo = tableInfo;
        }
        public TableMemberInfo(MemberInfo? memberInfo, TableInfo tableInfo)
        {
            this.memberInfo = memberInfo;
            TableInfo = tableInfo;
        }

        #region SQL
        public bool IsPrimary { get; protected set; }
        public bool IsAutoIncrement { get; protected set; }
        public bool IsNullable { get; protected set; }
        public string SqlTypeTxt { get; protected set; } = "";
        public DbType SqlType { get; protected set; }
        public string SqlName { get; protected set; } = "";

        public virtual object? GetSqlValue(object obj)
        {
            // TODO maybe check constraint here
            if (link == TableMemberInfoLink.None || link == TableMemberInfoLink.Parent)
            {
                return GetValue(obj);
            }
            else if (link == TableMemberInfoLink.Simple)
            {
                object? elementRef = GetValue(obj);
                if (elementRef is IStorable storableLink)
                {
                    return storableLink.id;
                }
            }
            return null;
        }

        public virtual void SetSqlValue(object obj, string value)
        {
            if (obj == null)
            {
                return;
            }
            Type? type = Type;
            if (type == null)
            {
                return;
            }
            if (type == typeof(int))
            {
                int nb;
                int.TryParse(value, out nb);
                SetValue(obj, nb);
            }
            else if (type == typeof(double))
            {
                double nb;
                double.TryParse(value, out nb);
                SetValue(obj, nb);
            }
            else if (type == typeof(float))
            {
                float nb;
                float.TryParse(value, out nb);
                SetValue(obj, nb);
            }
            else if (type == typeof(decimal))
            {
                decimal nb;
                decimal.TryParse(value, out nb);
                SetValue(obj, nb);
            }
            else if (type == typeof(string))
            {
                SetValue(obj, value);
            }
            else if (type == typeof(bool))
            {
                if (value == "1")
                {
                    SetValue(obj, true);
                }
                else
                {
                    SetValue(obj, false);
                }
            }
            else if (type == typeof(DateTime))
            {

            }
            else if (type.IsEnum)
            {
                SetValue(obj, Enum.Parse(type, value.ToString()));
            }
            else
            {
                // it's link
                if (string.IsNullOrEmpty(value))
                {
                    SetValue(obj, null);
                }
                else
                {
                    // TODO load reference field
                   // SetValue(obj, value);
                }
            }

        }

        #region link
        public TableMemberInfoLink link { get; protected set; } = TableMemberInfoLink.None;
        public TableInfo? TableLinked { get; set; }
        public Type? TableLinkedType { get; protected set; }

        #endregion
        public TableMemberInfo TransformForParentLink(TableInfo parentTable)
        {
            TableMemberInfo parentLink = new TableMemberInfo(memberInfo, TableInfo);
            parentLink.PrepareForSQL();
            parentLink.link = TableMemberInfoLink.Parent;
            parentLink.TableLinked = parentTable;
            parentLink.IsPrimary = true;
            parentLink.IsAutoIncrement = false;
            parentLink.IsNullable = false;
            return parentLink;
        }
        public bool PrepareForSQL()
        {
            if (memberInfo != null)
            {
                SqlName = memberInfo.Name;
                PrepareAttributesForSQL();
            }
            return PrepareTypeForSQL();
        }
        protected bool PrepareTypeForSQL()
        {
            bool isOk = false;
            Type? type = Type;
            if (type == null)
            {
                return false;
            }
            if (type == typeof(int))
            {
                SqlTypeTxt = "int";
                SqlType = DbType.Int32;
                ForeignKey? attr = GetCustomAttribute<ForeignKey>();
                if (attr != null)
                {
                    if (IsTypeUsable(attr.type))
                    {
                        link = TableMemberInfoLink.Simple;
                        TableLinkedType = attr.type;
                    }
                    else
                    {
                        string errorTxt = "Can't use type " + attr.type.FullName + " as foreign key inside " + TableInfo.SqlTableName;
                        new DataError(DataErrorCode.TypeNotStorable, errorTxt).Print();
                        return false;
                    }
                }
                isOk = true;
            }
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                SqlTypeTxt = "float";
                SqlType = DbType.Double;
                isOk = true;
            }
            else if (type == typeof(string))
            {
                SqlType = DbType.String;
                Size? attr = GetCustomAttribute<Size>();
                if (attr != null)
                {
                    if (attr.max)
                    {
                        SqlTypeTxt = "varchar(MAX)";
                    }
                    else
                    {
                        SqlTypeTxt = "varchar(" + attr.nb + ")";
                    }
                }
                else
                {
                    SqlTypeTxt = "varchar(255)";
                }
                isOk = true;
            }
            else if (type == typeof(Boolean))
            {
                SqlType = DbType.Boolean;
                SqlTypeTxt = "bit";
                isOk = true;
            }
            else if (type == typeof(DateTime))
            {
                SqlType = DbType.DateTime;
                SqlTypeTxt = "datetime";
                isOk = true;
            }
            else if (type.IsEnum)
            {
                SqlType = DbType.String;
                SqlTypeTxt = "varchar(255)";
                isOk = true;
            }
            else if (IsTypeUsable(type))
            {
                SqlType = DbType.Int32;
                link = TableMemberInfoLink.Simple;
                TableLinkedType = type;
                SqlTypeTxt = "int";
                isOk = true;
            }
            else
            {
                // TODO maybe implement both side for N-M link
                Type? refType = IsListTypeUsable(type);
                if (refType != null)
                {
                    link = TableMemberInfoLink.Multiple;
                    TableLinkedType = refType;
                    isOk = true;
                }
                else
                {
                    refType = IsDictionaryTypeUsable(type);
                    if (refType != null)
                    {
                        link = TableMemberInfoLink.Multiple;
                        TableLinkedType = refType;
                        isOk = true;
                    }
                }
            }
            return isOk;
        }

        protected void PrepareAttributesForSQL()
        {
            List<object> attributes = GetCustomAttributes(false);
            foreach (object attribute in attributes)
            {
                if (attribute is Primary)
                {
                    IsPrimary = true;
                }
                else if (attribute is AutoIncrement)
                {
                    IsAutoIncrement = true;
                }
                else if (attribute is Attributes.Nullable)
                {
                    IsNullable = true;
                }
            }

        }
        protected bool IsTypeUsable(Type type)
        {
            if (type == null)
            {
                return false;
            }
            return type.GetInterfaces().Contains(typeof(IStorable));
        }
        protected Type? IsListTypeUsable(Type type)
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
        protected Type? IsDictionaryTypeUsable(Type type)
        {
            if (type.IsGenericType && type.GetInterfaces().Contains(typeof(IDictionary)))
            {
                Type typeIndex = type.GetGenericArguments()[0];
                if (typeIndex == typeof(Int32))
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
        #endregion



        #region merge info
        /// <summary>
        /// Interface for Type
        /// </summary>
        public virtual Type? Type
        {
            get
            {
                if (memberInfo is FieldInfo fieldInfo)
                {
                    return fieldInfo.FieldType;
                }
                else if (memberInfo is PropertyInfo propertyInfo)
                {
                    return propertyInfo.PropertyType;
                }
                return null;
            }
        }
        /// <summary>
        /// Interface for Name
        /// </summary>
        public virtual string Name
        {
            get
            {
                if (memberInfo != null)
                {
                    return memberInfo.Name;
                }
                return "";
            }
        }
        /// <summary>
        /// Interface for GetCustomAttribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T? GetCustomAttribute<T>() where T : Attribute
        {
            return memberInfo?.GetCustomAttribute<T>();
        }
        /// <summary>
        /// Interface for GetCustomAttributes
        /// </summary>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public virtual List<object> GetCustomAttributes(bool inherit)
        {
            try
            {
                if (memberInfo is FieldInfo fieldInfo)
                {
                    return fieldInfo.GetCustomAttributes(inherit).ToList();
                }
                else if (memberInfo is PropertyInfo propertyInfo)
                {
                    return propertyInfo.GetCustomAttributes(inherit).ToList();

                }
            }
            catch (Exception e)
            {
                new DataError(DataErrorCode.UnknowError, e).Print();
            }
            return new List<object>();

        }

        /// <summary>
        /// Interface for GetValue
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual object? GetValue(object obj)
        {
            try
            {
                MemberInfo? member = GetRealMemmber(obj);
                if (member is FieldInfo fieldInfo)
                {
                    return fieldInfo.GetValue(obj);
                }
                else if (member is PropertyInfo propertyInfo)
                {
                    return propertyInfo.GetValue(obj);

                }
            }
            catch (Exception e)
            {
                new DataError(DataErrorCode.UnknowError, e).Print();
            }
            return null;

        }
        /// <summary>
        /// Interface for set value
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public virtual void SetValue(object obj, object? value)
        {
            try
            {
                MemberInfo? member = GetRealMemmber(obj);
                if (member is FieldInfo fieldInfo)
                {
                    fieldInfo.SetValue(obj, value);
                }
                else if (member is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(obj, value);

                }
            }
            catch (Exception e)
            {
                new DataError(DataErrorCode.UnknowError, e).Print();
            }
        }
        /// <summary>
        /// Interface for ReflectedType
        /// </summary>
        public virtual Type? ReflectedType
        {
            get
            {
                if (memberInfo is FieldInfo fieldInfo)
                {
                    return fieldInfo.ReflectedType;
                }
                else if (memberInfo is PropertyInfo propertyInfo)
                {
                    return propertyInfo.ReflectedType;
                }
                return null;

            }
        }

        /// <summary>
        /// Avoid Getting a member with generic param
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected MemberInfo? GetRealMemmber(object obj)
        {
            MemberInfo? member = memberInfo;
            if (member != null && TableInfo.IsAbstract)
            {
                Type typeToUse = obj.GetType();
                if (!memberInfoByType.ContainsKey(typeToUse))
                {
                    MemberInfo[] members = typeToUse.GetMember(member.Name);
                    memberInfoByType.Add(typeToUse, members[0]);
                }
                member = memberInfoByType[typeToUse];
            }
            return member;
        }

        #endregion

        public override string ToString()
        {
            string attrs = "";
            List<object> attributes = GetCustomAttributes(true);
            if (attributes.Count > 0)
            {
                attrs = "- " + string.Join(", ", attributes.Select(a => "[" + a.GetType().Name + "]"));
            }
            Type? type = Type;
            if (type != null)
            {
                string typeTxt = type.Name;
                if (!TypeTools.primitiveType.Contains(Type))
                {
                    typeTxt += " - " + type.Assembly.GetName().Name;
                }
                return Name + " (" + typeTxt + ") " + attrs;
            }
            return Name + "(NULL)";
        }
    }
}
