using AventusSharp.Data.Attributes;
using AventusSharp.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data;
using AventusSharp.Data.Manager;
using AventusSharp.Attributes.Data;
using System.Linq.Expressions;

namespace AventusSharp.Data.Storage.Default
{
    public enum TableMemberInfoLink
    {
        None,
        Simple,
        SimpleInt,
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
        public bool IsUnique { get; internal set; }
        public string SqlTypeTxt { get; protected set; } = "";
        public DbType SqlType { get; protected set; }
        public string SqlName { get; protected set; } = "";

        public bool IsAutoCreate { get; protected set; } = false;
        public bool IsAutoUpdate { get; protected set; } = false;
        public bool IsAutoDelete { get; protected set; } = false;
        public bool IsAutoRead { get; protected set; } = false;

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
                    return storableLink.Id;
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
                SetValue(obj, DateTime.Parse(value));
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
            parentLink.IsUnique = false;
            return parentLink;
        }
        public VoidWithDataError PrepareForSQL()
        {
            if (memberInfo != null)
            {
                SqlName = memberInfo.Name;
                PrepareAttributesForSQL();
            }
            return PrepareTypeForSQL();
        }
        protected VoidWithDataError PrepareTypeForSQL()
        {
            VoidWithDataError result = new VoidWithDataError();
            Type? type = Type;
            if (type == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.FieldTypeNotFound, "Can't found a type for " + SqlName));
                return result;
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
                        Link = TableMemberInfoLink.SimpleInt;
                        TableLinkedType = attr.Type;
                    }
                    else
                    {
                        string errorTxt = "Can't use type " + attr.Type.FullName + " as foreign key inside " + TableInfo.SqlTableName;
                        result.Errors.Add(new DataError(DataErrorCode.TypeNotStorable, errorTxt));
                        return result;
                    }
                }
            }
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                SqlTypeTxt = "float";
                SqlType = DbType.Double;
            }
            else if (type == typeof(string))
            {
                SqlType = DbType.String;
                Size? attr = GetCustomAttribute<Size>();
                if (attr != null)
                {
                    if (attr.SizeType == null)
                        SqlTypeTxt = "varchar(" + attr.Max + ")";
                    else if (attr.SizeType == SizeEnum.MaxVarChar)
                        SqlTypeTxt = "varchar(MAX)";
                    else if (attr.SizeType == SizeEnum.Text)
                        SqlTypeTxt = "TEXT";
                    else if (attr.SizeType == SizeEnum.MediumText)
                        SqlTypeTxt = "MEDIUMTEXT";
                    else if (attr.SizeType == SizeEnum.LongText)
                        SqlTypeTxt = "LONGTEXT";
                }
                else
                {
                    SqlTypeTxt = "varchar(255)";
                }
            }
            else if (type == typeof(Boolean))
            {
                SqlType = DbType.Boolean;
                SqlTypeTxt = "bit";
            }
            else if (type == typeof(DateTime))
            {
                SqlType = DbType.DateTime;
                SqlTypeTxt = "datetime";
            }
            else if (type.IsEnum)
            {
                SqlType = DbType.String;
                SqlTypeTxt = "varchar(255)";
            }
            else if (IsTypeUsable(type))
            {
                SqlType = DbType.Int32;
                Link = TableMemberInfoLink.Simple;
                TableLinkedType = type;
                SqlTypeTxt = "int";
            }
            else
            {
                // TODO maybe implement both side for N-M link
                Type? refType = IsListTypeUsable(type);
                if (refType != null)
                {
                    Link = TableMemberInfoLink.Multiple;
                    TableLinkedType = refType;
                }
                else
                {
                    refType = IsDictionaryTypeUsable(type);
                    if (refType != null)
                    {
                        Link = TableMemberInfoLink.Multiple;
                        TableLinkedType = refType;
                    }
                }
            }
            return result;
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
                else if (attribute is AutoRead)
                {
                    IsAutoRead = true;
                }
                else if (attribute is AutoCUD)
                {
                    IsAutoCreate = true;
                    IsAutoUpdate = true;
                    IsAutoDelete = true;
                }
                else if (attribute is AutoCRUD)
                {
                    IsAutoCreate = true;
                    IsAutoUpdate = true;
                    IsAutoDelete = true;
                    IsAutoRead = true;
                }
                else if (attribute is Unique)
                {
                    IsUnique = true;
                }
                else if (attribute is SqlName attrSqlName)
                {
                    SqlName = attrSqlName.Name;
                }
                else if (attribute is ValidationAttribute validationAttribute)
                {
                    ValidationAttributes.Add(validationAttribute);
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


        #region Reverse Link
        public Func<int, ResultWithDataError<List<IStorable>>>? reverseQueryBuilder;
        public TableMemberInfo? reverseMember;
        public Type? ReverseLinkType;
        private ReverseLink? ReverseLinkAttr;
        public VoidWithDataError SetReverseLink(ReverseLink attr)
        {
            ReverseLinkAttr = attr;
            VoidWithDataError result = new();
            if (memberInfo != null)
            {
                Type? type = IsListTypeUsable(Type);
                if (type == null)
                {
                    type = IsDictionaryTypeUsable(Type);
                    if (type == null)
                    {
                        type = Type;
                    }
                }
                ReverseLinkType = type;

            }
            else
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Impossible case"));
            }
            return result;
        }
        public VoidWithDataError PrepareReverseLink(TableInfo tableInfo)
        {
            VoidWithDataError result = new();
            TableLinked = tableInfo;
            PrepareAttributesForSQL();
            if (ReverseLinkAttr?.field != null)
            {
                TableMemberInfo? reversInfo = null;
                TableInfo? el = tableInfo;
                while (el != null)
                {
                    reversInfo = tableInfo.Members.Find(m => m.Name == ReverseLinkAttr.field);
                    if (reversInfo == null)
                    {
                        el = tableInfo.Parent; continue;
                    }
                    break;
                }
                if (reversInfo == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.MemberNotFound, "The name " + ReverseLinkAttr.field + " can't be found on " + tableInfo.SqlTableName));
                }
                else
                {
                    this.reverseMember = reversInfo;
                }
            }
            else
            {
                List<TableMemberInfo> reversInfo = new List<TableMemberInfo>();
                TableInfo? el = tableInfo;
                while (el != null)
                {
                    reversInfo.AddRange(tableInfo.Members.Where(m => m.TableLinkedType == TableInfo.Type).ToList());
                    el = tableInfo.Parent;
                }
                if (reversInfo.Count > 1)
                {
                    result.Errors.Add(new DataError(DataErrorCode.TooMuchMemberFound, "Too much matching type " + TableInfo.Type + " on type " + tableInfo.SqlTableName));
                }
                else if (reversInfo.Count == 0)
                {
                    result.Errors.Add(new DataError(DataErrorCode.MemberNotFound, "The type " + TableInfo.Type + " can't be found on " + tableInfo.SqlTableName));
                }
                else
                {
                    this.reverseMember = reversInfo[0];
                }
            }
            return result;
        }
        public VoidWithDataError ReverseQuery(int id, object o)
        {
            VoidWithDataError result = new();
            ResultWithDataError<List<IStorable>> resultTemp = ReverseQuery(id);
            if(!resultTemp.Success)
            {
                result.Errors = resultTemp.Errors;
                return result;
            }
            SetValue(o, resultTemp.Result);
            return result;
        }
        public ResultWithDataError<List<IStorable>> ReverseQuery(int id)
        {
            ResultWithDataError<List<IStorable>> result = new();
            if (reverseQueryBuilder == null)
            {
                if (ReverseLinkType == null || reverseMember == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.ReverseLinkNotExist, "Reverse link seems to not be init"));
                    return result;
                }

                ParameterExpression argParam = Expression.Parameter(ReverseLinkType, "t");
                Expression nameProperty;
                Type varType;
                if (TypeTools.PrimitiveType.Contains(reverseMember.Type))
                {
                    varType = reverseMember.Type;
                    nameProperty = Expression.Property(argParam, reverseMember.SqlName);
                }
                else
                {
                    varType = typeof(int);
                    Expression temp = Expression.Property(argParam, "el");
                    nameProperty = Expression.Property(temp, reverseMember.SqlName);
                }
                Expression<Func<int>> idLambda = () => id;
                var var1 = Expression.Variable(varType, Storable.Id);

                Expression e1 = Expression.Equal(nameProperty, idLambda.Body);
                LambdaExpression lambda = Expression.Lambda(e1, argParam);

                IGenericDM dm = GenericDM.Get(ReverseLinkType);
                object? query = dm.GetType().GetMethod("CreateQuery")?.MakeGenericMethod(reverseMember.TableInfo.Type).Invoke(dm, null);
                if (query == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.ErrorCreatingReverseQuery, "Can't create the query"));
                    return result;
                }
                MethodInfo? setVariable = query.GetType().GetMethod("SetVariable");
                if (setVariable == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.ErrorCreatingReverseQuery, "Can't get the function setVariable"));
                    return result;
                }
                MethodInfo? runWithError = query.GetType().GetMethod("RunWithError");
                if (runWithError == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.ErrorCreatingReverseQuery, "Can't get the function runWithError"));
                    return result;
                }
                MethodInfo? whereWithParam = query.GetType().GetMethod("WhereWithParameters");
                if (whereWithParam == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.ErrorCreatingReverseQuery, "Can't get the function whereWithParam"));
                    return result;
                }
                whereWithParam.Invoke(query, new object[] { lambda });


                reverseQueryBuilder = delegate (int id)
                {
                    ResultWithDataError<List<IStorable>> result = new();
                    setVariable.Invoke(query, new object[] { Storable.Id, id });
                    IResultWithError? resultWithError = (IResultWithError?)runWithError.Invoke(query, null);
                    if (resultWithError != null)
                    {
                        foreach (GenericError error in resultWithError.Errors)
                        {
                            if (error is DataError dataError)
                            {
                                result.Errors.Add(dataError);
                            }
                        }
                        result.Result = new List<IStorable>();
                        if(resultWithError.Result is IList list)
                        {
                            foreach(object item in list)
                            {
                                if(item is IStorable storable)
                                {
                                    result.Result.Add(storable);
                                }
                            }
                        }
                    }
                    return result;
                };
            }
            result = reverseQueryBuilder(id);
            return result;
        }
        public void SetReverseId(object o, int id)
        {
            if(reverseMember == null)
            {
                return;
            }

            // check if int id or object
            if(IsTypeUsable(reverseMember.Type))
            {
                IStorable el = TypeTools.CreateNewObj<IStorable>(reverseMember.Type);
                el.Id = id;
                reverseMember.SetValue(o, el);
            }
            else
            {
                reverseMember.SetValue(o, id);
            }
        }
        #endregion

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
                MemberInfo? member = GetRealMember(obj);
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
                if (Link == TableMemberInfoLink.Simple && value is int)
                {
                    SetIntValueToStorable(obj, value);
                    return;
                }

                MemberInfo? member = GetRealMember(obj);
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

        private void SetIntValueToStorable(object obj, object? value)
        {
            object? temp = GetValue(obj);
            if (temp == null)
            {
                temp = Activator.CreateInstance(Type);
                if (temp == null)
                {
                    throw new Exception("Can't create the type " + Type.Name);
                }
                SetValue(obj, temp);
            }
            TableLinked?.Primary?.SetValue(temp, value);
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
        protected MemberInfo? GetRealMember(object obj)
        {
            try
            {
                MemberInfo? member = memberInfo;
                if (member?.ReflectedType?.IsGenericType == true)
                {
                    Type typeToUse = obj.GetType();
                    if (!memberInfoByType.ContainsKey(typeToUse))
                    {
                        MemberInfo[] members = typeToUse.GetMember(member.Name);
                        if (members.Length == 0)
                        {
                            return null;
                        }
                        memberInfoByType.Add(typeToUse, members[0]);
                    }
                    member = memberInfoByType[typeToUse];
                }

                if (member == null || member.ReflectedType == null || !member.ReflectedType.IsInstanceOfType(obj))
                {
                    return null;
                }

                return member;
            }
            catch (Exception e)
            {
                new DataError(DataErrorCode.UnknowError, e).Print();
            }
            return null;
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
