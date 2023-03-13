using AvenutsSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default
{
    public class TableInfo
    {
        public static string GetSQLTableName(Type type)
        {
            return type.Name.Split('`')[0];
        }

        /// <summary>
        /// The name of the class
        /// </summary>
        public string SqlTableName { get; private set; }

        /// <summary>
        /// The class parent, for heritance
        /// </summary>
        public TableInfo Parent { get; internal set; }

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
        public List<TableMemberInfo> members { get; private set; } = new List<TableMemberInfo>();

        /// <summary>
        /// List of primaries members for this class only
        /// </summary>
        /// <remarks></remarks>
        public List<TableMemberInfo> primaries { get => members.Where(m => m.IsPrimary).ToList(); }

        public TableInfo(PyramidInfo pyramid)
        {
            this.SqlTableName = TableInfo.GetSQLTableName(pyramid.type);
            if (pyramid.type.IsGenericType)
            {
                this.IsAbstract = true;
            }
            this.LoadMembers(pyramid.type);

        }

        private void LoadMembers(Type type)
        {
            foreach (FieldInfo field in type.GetFields())
            {
                if (field.DeclaringType == type)
                {
                    TableMemberInfo temp = new TableMemberInfo(field, this);
                    prepareMembers(temp);
                }
            }
            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.DeclaringType == type)
                {
                    TableMemberInfo temp = new TableMemberInfo(property, this);
                    prepareMembers(temp);
                }
            }
        }
        private void prepareMembers(TableMemberInfo temp)
        {
            if (!temp.GetCustomAttributes(false).Any(o => o is NotInDB))
            {
                if (temp.PrepareForSQL())
                {
                    members.Add(temp);
                }
            }
        }
    }
}
