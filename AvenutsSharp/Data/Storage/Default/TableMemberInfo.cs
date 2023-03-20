using AventusSharp.Attributes;
using AventusSharp.Log;
using AventusSharp.Tools;
using AvenutsSharp.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AventusSharp.Data.Storage.Default
{
    public enum TableMemberInfoLink
    {
        None,
        Simple,
        Multiple,
    }
    public class TableMemberInfo
    {
        private MemberInfo memberInfo;
        public readonly TableInfo TableInfo;
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
        public TableMemberInfo(MemberInfo memberInfo, TableInfo tableInfo)
        {
            this.memberInfo = memberInfo;
            TableInfo = tableInfo;
        }

        #region SQL
        public bool IsPrimary { get; private set; }
        public bool IsAutoIncrement { get; private set; }
        public bool IsNullable { get; private set; }
        public bool IsParentLink { get; private set; }
        public string SqlType { get; private set; }
        public string SqlName { get; private set; }

        public string GetSqlValue(object obj)
        {
            if (obj == null)
            {
                return "NULL";
            }

            obj = this.GetValue(obj);
            if (obj == null)
            {
                return "NULL";
            }

            if (SqlType.StartsWith("varchar"))
            {

            }

            if (SqlType == "bit")
            {
                if ((bool)obj)
                {
                    return "1";
                }
                return "0";
            }
            if (SqlType == "float")
            {
                return obj.ToString().Replace(",", ".");
            }


            return obj.ToString();
        }

        public void SetSqlValue(object obj, string value)
        {
            if (obj == null)
            {
                return;
            }
            Type type = Type;
            if (type == typeof(Int32))
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
            else if(type == typeof(float))
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
                SetValue(obj, value);
            }

        }

        #region link
        public TableMemberInfoLink link { get; private set; } = TableMemberInfoLink.None;
        public TableInfo TableLinked { get; set; }
        public Type TableLinkedType { get; private set; }

        #endregion
        public TableMemberInfo TransformForParentLink(TableInfo parentTable)
        {
            TableMemberInfo parentLink = new TableMemberInfo(memberInfo, TableInfo);
            parentLink.PrepareForSQL();
            parentLink.link = TableMemberInfoLink.Simple;
            parentLink.TableLinked = parentTable;
            parentLink.IsPrimary = true;
            parentLink.IsAutoIncrement = false;
            parentLink.IsNullable = false;
            parentLink.IsParentLink = true;
            return parentLink;
        }
        public bool PrepareForSQL()
        {

            SqlName = memberInfo.Name;
            PrepareAttributesForSQL();
            return PrepareTypeForSQL();
        }
        private bool PrepareTypeForSQL()
        {
            bool isOk = false;
            Type type = Type;
            if (type == typeof(Int32))
            {
                SqlType = "int";
                ForeignKey attr = GetCustomAttribute<ForeignKey>();
                if (attr != null)
                {
                    if (IsTypeUsable(attr.type))
                    {
                        link = TableMemberInfoLink.Simple;
                        TableLinkedType = attr.type;
                    }
                    else
                    {
                        Console.WriteLine("Can't use type " + attr.type.FullName + " as foreign key");
                    }
                }
                isOk = true;
            }
            else if (type == typeof(Double) || type == typeof(Single) || type == typeof(Decimal))
            {
                SqlType = "float";
                isOk = true;
            }
            else if (type == typeof(String))
            {
                Size attr = GetCustomAttribute<Size>();
                if (attr != null)
                {
                    if (attr.max)
                    {
                        SqlType = "varchar(MAX)";
                    }
                    else
                    {
                        SqlType = "varchar(" + attr.nb + ")";
                    }
                }
                else
                {
                    SqlType = "varchar(255)";
                }
                isOk = true;
            }
            else if (type == typeof(Boolean))
            {
                SqlType = "bit";
                isOk = true;
            }
            else if (type == typeof(DateTime))
            {
                // TODO change it to use real Date
                SqlType = "varchar(255)";
                isOk = true;
            }
            else if (type.IsEnum)
            {
                SqlType = "varchar(255)";
                isOk = true;
            }
            else if (IsTypeUsable(type))
            {
                link = TableMemberInfoLink.Simple;
                TableLinkedType = type;
                SqlType = "int";
                isOk = true;
            }
            else
            {
                // TODO maybe implement both side for N-M link
                Type refType = IsListTypeUsable(type);
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
        private void PrepareAttributesForSQL()
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
        private bool IsTypeUsable(Type type)
        {
            if (type == null)
            {
                return false;
            }
            return type.GetInterfaces().Contains(typeof(IStorable));
        }
        private Type IsListTypeUsable(Type type)
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
        private Type IsDictionaryTypeUsable(Type type)
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
        public Type Type
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
        public string Name
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
        public T GetCustomAttribute<T>() where T : Attribute
        {
            return memberInfo.GetCustomAttribute<T>();
        }
        /// <summary>
        /// Interface for GetCustomAttributes
        /// </summary>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public List<object> GetCustomAttributes(bool inherit)
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
                LogError.getInstance().WriteLine(e);
            }
            return new List<object>();

        }
        /// <summary>
        /// Interface for GetValue
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public object GetValue(object obj)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                return fieldInfo.GetValue(obj);
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                return propertyInfo.GetValue(obj);

            }
            return null;

        }
        /// <summary>
        /// Interface for set value
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public void SetValue(object obj, object value)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                fieldInfo.SetValue(obj, value);
            }
            else if (memberInfo is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(obj, value);

            }
        }
        /// <summary>
        /// Interface for ReflectedType
        /// </summary>
        public Type ReflectedType
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
        #endregion

        public override string ToString()
        {
            string attrs = "";
            List<object> attributes = GetCustomAttributes(true);
            if (attributes.Count > 0)
            {
                attrs = "- " + string.Join(", ", attributes.Select(a => "[" + a.GetType().Name + "]"));
            }
            string type = Type.Name;
            if (!TypeTools.primitiveType.Contains(Type))
            {
                type += " - " + Type.Assembly.GetName().Name;
            }
            return Name + " (" + type + ") " + attrs;
        }
    }
}
