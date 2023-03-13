using AventusSharp.Log;
using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    public class DataMemberInfo
    {
        private MemberInfo memberInfo;
        public DataMemberInfo(FieldInfo fieldInfo)
        {
            memberInfo = fieldInfo;
        }
        public DataMemberInfo(PropertyInfo propertyInfo)
        {
            memberInfo = propertyInfo;
        }

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
