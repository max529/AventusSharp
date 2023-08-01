using AventusSharp.Data.Manager;
using AventusSharp.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AventusSharp.Data.Storage.Default
{
    public class TableInfo
    {
        public readonly static string TypeIdentifierName = "__type";
        public static string GetSQLTableName(Type type)
        {
            return DataMainManager.Config.GetSQLTableName(type);
        }

        /// <summary>
        /// The name of the class
        /// </summary>
        public string SqlTableName { get; private set; }

        /// <summary>
        /// The class parent, for heritance
        /// </summary>
        public TableInfo? Parent { get; internal set; }

        /// <summary>
        /// The class children, for heritance
        /// </summary>
        public List<TableInfo> Children { get; private set; } = new List<TableInfo>();

        /// <summary>
        /// Is the class abstract ?
        /// </summary>
        public bool IsAbstract { get; private set; }

        /// <summary>
        /// Has the class an infinite loop
        /// </summary>
        /// <remarks>True if the class itself or a field contains a reference on itself</remarks>
        public bool IsInfinite { get; private set; }

        /// <summary>
        /// List of members for this class only
        /// </summary>
        /// <remarks></remarks>
        public List<TableMemberInfo> Members { get; private set; } = new List<TableMemberInfo>();

        public TableMemberInfo? TypeMember { get; private set; } = null;

        /// <summary>
        /// List of primaries members for this class only
        /// </summary>
        /// <remarks></remarks>
        public TableMemberInfo? Primary { get; set; }

        public Type Type { get; private set; }

        public IGenericDM? DM { get; private set; }

        public TableInfo(PyramidInfo pyramid)
        {
            SqlTableName = GetSQLTableName(pyramid.type);
            Type typeToLoad = pyramid.type;
            Type = typeToLoad;
            if (pyramid.type.IsGenericType)
            {
                IsAbstract = true;
            }
            LoadMembers(typeToLoad);
        }

        public VoidWithError LoadDM()
        {
            VoidWithError result = new VoidWithError();
            var resultTemp = GenericDM.GetWithError(Type);
            if(resultTemp.Success && resultTemp.Result != null)
            {
                DM = resultTemp.Result;
            }
            else
            {
                result.Errors.AddRange(resultTemp.Errors);
            }
            return result;
        }

        public void AddTypeMember()
        {
            CustomTableMemberInfo typeMember = new(TypeIdentifierName, typeof(string), this);
            typeMember.DefineSQLInformation(new SQLInformation()
            {
                SqlName = TypeIdentifierName,
                SqlType = System.Data.DbType.String,
                SqlTypeTxt = "varchar(255)",
            });
            typeMember.DefineGetValue(delegate (object obj)
            {
                return obj.GetType().AssemblyQualifiedName;
            });
            typeMember.IsUpdatable = false;
            this.TypeMember = typeMember;
            Members.Insert(0, typeMember);
        }

        private void LoadMembers(Type type)
        {
            foreach (FieldInfo field in type.GetFields())
            {
                if (field.DeclaringType == type)
                {
                    TableMemberInfo temp = new(field, this);
                    PrepareMembers(temp);
                }
            }
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.DeclaringType == type)
                {
                    TableMemberInfo temp = new(property, this);
                    PrepareMembers(temp);
                }
            }
        }
        private void PrepareMembers(TableMemberInfo temp)
        {
            if (!temp.GetCustomAttributes(false).Any(o => o is NotInDB))
            {
                if (temp.PrepareForSQL())
                {
                    Members.Add(temp);
                    if (temp.IsPrimary)
                    {
                        Primary = temp;
                    }
                }
            }
        }

        public bool IsChildOf(object o)
        {
            Type O = o.GetType();
            if (Type.IsGenericType)
            {
                try
                {
                    Type T = Type.MakeGenericType(O);
                    return O.IsSubclassOf(T);
                }
                catch
                {
                    return false;
                }
            }
            return O == Type;
        }
    }
}
