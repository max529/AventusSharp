using AventusSharp.Attributes;
using AventusSharp.Tools;
using AvenutsSharp.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data;
using AventusSharp.Data.Manager;

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
            if (type == typeof(Boolean))
                return DbType.Boolean;
            if (type == typeof(DateTime))
                return DbType.DateTime;
            if (type.IsEnum)
                return DbType.String;
            if (IsTypeUsable(type))
                return DbType.Int32;
            return null;
        }
        protected MemberInfo? memberInfo;
        protected Dictionary<Type, MemberInfo> memberInfoByType = new();
        public TableInfo TableInfo { get; private set; }
        public IGenericDM? DM { get => TableInfo.DM; }
        public TableMemberInfo(TableInfo tableInfo)
        {
            TableInfo = tableInfo;
        }
        public TableMemberInfo(FieldInfo fieldInfo, TableInfo tableInfo) : this(tableInfo)
        {
            memberInfo = fieldInfo;
        }
        public TableMemberInfo(PropertyInfo propertyInfo, TableInfo tableInfo) : this(tableInfo)
        {
            memberInfo = propertyInfo;
        }
        public TableMemberInfo(MemberInfo? memberInfo, TableInfo tableInfo) : this(tableInfo)
        {
            this.memberInfo = memberInfo;
        }

        public void ChangeTableInfo(TableInfo tableInfo)
        {
            TableInfo = tableInfo;
        }

        #region SQL
        public bool IsPrimary { get; protected set; }
        public bool IsAutoIncrement { get; protected set; }
        public bool IsNullable { get; protected set; }
        public bool IsDeleteOnCascade { get; protected set; }
        public bool IsUpdatable { get; internal set; } = true;
        public string SqlTypeTxt { get; protected set; } = "";
        public DbType SqlType { get; protected set; }
        public string SqlName { get; protected set; } = "";

        public bool IsAutoCreate { get; protected set; } = false;
        public bool IsAutoUpdate { get; protected set; } = false;
        public bool IsAutoDelete { get; protected set; } = false;
        private readonly List<ValidationAttribute> ValidationAttributes = new();

        public virtual object? GetSqlValue(object obj)
        {
            // TODO maybe check constraint here
            if (Link == TableMemberInfoLink.None || Link == TableMemberInfoLink.Parent)
            {
                return GetValue(obj);
            }
            else if (Link == TableMemberInfoLink.Simple)
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
                if (int.TryParse(value, out int nb))
                {
                    SetValue(obj, nb);
                }
            }
            else if (type == typeof(double))
            {
                if (double.TryParse(value, out double nb))
                {
                    SetValue(obj, nb);
                }
            }
            else if (type == typeof(float))
            {
                if (float.TryParse(value, out float nb))
                {
                    SetValue(obj, nb);
                }
            }
            else if (type == typeof(decimal))
            {
                if (decimal.TryParse(value, out decimal nb))
                {
                    SetValue(obj, nb);
                }
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
        public TableMemberInfoLink Link { get; protected set; } = TableMemberInfoLink.None;
        public TableInfo? TableLinked { get; set; }
        public Type? TableLinkedType { get; protected set; }

        #endregion
        public TableMemberInfo TransformForParentLink(TableInfo parentTable)
        {
            TableMemberInfo parentLink = new(memberInfo, TableInfo);
            parentLink.PrepareForSQL();
            parentLink.Link = TableMemberInfoLink.Parent;
            parentLink.TableLinked = parentTable;
            parentLink.IsPrimary = true;
            parentLink.IsAutoIncrement = false;
            parentLink.IsNullable = false;
            parentLink.IsUpdatable = false;
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
                    if (IsTypeUsable(attr.Type))
                    {
                        Link = TableMemberInfoLink.Simple;
                        TableLinkedType = attr.Type;
                    }
                    else
                    {
                        string errorTxt = "Can't use type " + attr.Type.FullName + " as foreign key inside " + TableInfo.SqlTableName;
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
                    if (attr.Max)
                    {
                        SqlTypeTxt = "varchar(MAX)";
                    }
                    else
                    {
                        SqlTypeTxt = "varchar(" + attr.Nb + ")";
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
                Link = TableMemberInfoLink.Simple;
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
                    Link = TableMemberInfoLink.Multiple;
                    TableLinkedType = refType;
                    isOk = true;
                }
                else
                {
                    refType = IsDictionaryTypeUsable(type);
                    if (refType != null)
                    {
                        Link = TableMemberInfoLink.Multiple;
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
            IsNullable = DataMainManager.Config?.nullByDefault ?? false;
            foreach (object attribute in attributes)
            {
                if (attribute is Primary)
                {
                    IsPrimary = true;
                    IsUpdatable = false;
                }
                else if (attribute is AutoIncrement)
                {
                    IsAutoIncrement = true;
                }
                else if (attribute is Attributes.Nullable)
                {
                    IsNullable = true;
                }
                else if (attribute is NotNullable notNullable)
                {
                    IsNullable = false;
                    ValidationAttributes.Add(notNullable);
                }
                else if (attribute is DeleteOnCascade)
                {
                    IsDeleteOnCascade = true;
                }
                else if (attribute is AutoCreate)
                {
                    IsAutoCreate = true;
                }
                else if (attribute is AutoUpdate)
                {
                    IsAutoUpdate = true;
                }
                else if (attribute is AutoDelete)
                {
                    IsAutoDelete = true;
                }
                else if (attribute is AutoCUD)
                {
                    IsAutoCreate = true;
                    IsAutoUpdate = true;
                    IsAutoDelete = true;
                }
            }

        }
        protected static bool IsTypeUsable(Type type)
        {
            if (type == null)
            {
                return false;
            }
            return type.GetInterfaces().Contains(typeof(IStorable));
        }
        protected static Type? IsListTypeUsable(Type type)
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
        protected static Type? IsDictionaryTypeUsable(Type type)
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

        public List<string> IsValid(object? o)
        {
            List<string> errors = new();
            ValidationContext context = new(Name, Type);
            foreach (var validationAttribute in ValidationAttributes)
            {
                ValidationResult result = validationAttribute.IsValid(o, context);
                if (!string.IsNullOrEmpty(result.Msg))
                {
                    errors.Add(result.Msg);
                }
            }

            return errors;
        }

        #region merge info
        /// <summary>
        /// Interface for Type
        /// </summary>
        public virtual Type Type
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
                throw new Exception("No type for field??");
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
            if (member?.ReflectedType?.IsGenericType == true)
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
                if (!TypeTools.PrimitiveType.Contains(Type))
                {
                    typeTxt += " - " + type.Assembly.GetName().Name;
                }
                return Name + " (" + typeTxt + ") " + attrs;
            }
            return Name + "(NULL)";
        }
    }
}
