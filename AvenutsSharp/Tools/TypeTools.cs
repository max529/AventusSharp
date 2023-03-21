using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace AventusSharp.Tools
{
    public static class TypeTools
    {
        public static Type[] primitiveType
        {
            get => new Type[] {
                typeof(Enum),
                typeof(Decimal),
                typeof(String),
                typeof(Boolean),
                typeof(Byte),
                typeof(SByte),
                typeof(Int16),
                typeof(UInt16),
                typeof(Int32),
                typeof(UInt32),
                typeof(Int64),
                typeof(UInt64),
                typeof(IntPtr),
                typeof(UIntPtr),
                typeof(Char),
                typeof(Double),
                typeof(Single),
                typeof(DateTime)
            };
        }


        public static string GetReadableName(Type type)
        {
            string generic = "";
            if (type.IsGenericType)
            {
                generic = "<";
                Type[] arguments = type.GetGenericArguments();
                foreach (Type argument in arguments)
                {
                    generic += GetReadableName(argument);
                }
                generic += ">";
            }
            return type.Name.Split('`')[0] + generic;
        }

        public static object CreateNewObj(Type type)
        {
            NewExpression constructorExpression = Expression.New(type);
            Expression<Func<object>> lambdaExpression = Expression.Lambda<Func<object>>(constructorExpression);
            Func<object> createFunc = lambdaExpression.Compile();
            object newObj = createFunc();
            return newObj;
        }
        public static T CreateNewObj<T>()
        {
            return (T)CreateNewObj(typeof(T));
        }
    }
}
