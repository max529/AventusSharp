using AventusSharp.Data.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace AventusSharp.Data.Storage.Default.TableMember
{
    public class TableMemberInfoSqlNMInt : TableMemberInfoSql, ITableMemberInfoSqlLinkMultiple
    {
        public TableInfo? TableLinked { get; set; }
        public Type? TableLinkedType { get; protected set; }

        public string? TableIntermediateName
        {
            get
            {
                if (TableLinkedType == null)
                {
                    return null;
                }
                if (TableLinked == null)
                {
                    return TableInfo.SqlTableName + "_" + TableInfo.GetSQLTableName(TableLinkedType);
                }
                return TableInfo.SqlTableName + "_" + TableLinked.SqlTableName;
            }
        }
        public string? TableIntermediateKey1
        {
            get
            {
                return TableInfo.Primary == null ? null : TableInfo.SqlTableName + "_" + TableInfo.Primary.SqlName;
            }
        }

        public string? TableIntermediateKey2
        {
            get
            {
                if (TableLinkedType == null)
                {
                    return null;
                }
                if (TableLinked == null || TableLinked.Primary == null)
                {
                    return TableInfo.GetSQLTableName(TableLinkedType) + "_Id";
                }
                return TableLinked.SqlTableName + "_" + TableLinked.Primary.SqlName;
            }
        }

        public DbType LinkFieldType
        {
            get
            {
                if (TableLinkedType == null)
                {
                    return DbType.Int32;
                }
                if (TableLinked?.Primary is ITableMemberInfoSqlWritable linkPrimary)
                {
                    return linkPrimary.SqlType;
                }
                return DbType.Int32;
            }
        }

        public string LinkTableName
        {
            get
            {
                if (TableLinkedType == null)
                {
                    return "";
                }
                if (TableLinked == null)
                {
                    return TableInfo.GetSQLTableName(TableLinkedType);
                }
                return TableLinked.SqlTableName;
            }
        }

        public string LinkPrimaryName
        {
            get
            {
                if (TableLinkedType == null)
                {
                    return "";
                }
                if (TableLinked == null || TableLinked.Primary == null)
                {
                    return "Id";
                }
                return TableLinked.Primary.SqlName;
            }
        }

        public TableMemberInfoSqlNMInt(MemberInfo? memberInfo, TableInfo tableInfo, bool isNullable) : base(memberInfo, tableInfo, isNullable)
        {
        }

        public override VoidWithDataError PrepareForSQL()
        {
            VoidWithDataError result = new VoidWithDataError();
            if (memberInfo == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.MemberNotFound, "Member not found"));
                return result;
            }
            SqlName = memberInfo.Name;
            return result;
        }

        protected override bool ParseAttribute(Attribute attribute)
        {
            if (base.ParseAttribute(attribute))
            {
                return true;
            }

            if (attribute is ForeignKey foreignKeyAttr)
            {
                TableLinkedType = foreignKeyAttr.Type;
                return true;
            }
            return false;
        }


        public override object? GetSqlValue(object obj)
        {
            throw new System.NotImplementedException();
        }

        protected override void _SetSqlValue(object obj, string value)
        {
            List<int> ids = new();
            string[] splitted = value.Split(",");
            foreach (string s in splitted)
            {
                int id;
                if(int.TryParse(s, out id))
                {
                    ids.Add(id);
                }
            }
            SetValue(obj, ids);
        }

        protected override void ParseAttributes()
        {
            IsAutoCreate = false;
            IsAutoDelete = false;
            IsAutoUpdate = false;
            base.ParseAttributes();
        }
    }
}
