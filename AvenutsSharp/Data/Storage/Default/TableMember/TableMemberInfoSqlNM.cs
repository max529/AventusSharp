using AventusSharp.Data.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace AventusSharp.Data.Storage.Default.TableMember
{

    public class TableMemberInfoSqlNM : TableMemberInfoSql, ITableMemberInfoSqlLinkMultiple
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

        private IGenericDM? LinkDM;
        private MethodInfo? GetByIds;

        public TableMemberInfoSqlNM(MemberInfo? memberInfo, TableInfo tableInfo, bool isNullable) : base(memberInfo, tableInfo, isNullable)
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

            if (MemberType.GetInterfaces().Contains(typeof(IList)))
            {
                Type? refType = IsListTypeUsable(MemberType);
                if (refType != null)
                {
                    TableLinkedType = refType;
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.TypeNotStorable, "Type is not a storable type"));
                    return result;
                }
            }
            else if (MemberType.GetInterfaces().Contains(typeof(IDictionary)))
            {
                Type? refType = IsDictionaryTypeUsable(MemberType);
                if (refType != null)
                {
                    TableLinkedType = refType;
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.TypeNotStorable, "Type is not a storable type"));
                    return result;
                }
            }
            else
            {
                result.Errors.Add(new DataError(DataErrorCode.TypeNotStorable, "Type is not a storable type"));
                return result;
            }

            LinkDM = GenericDM.Get(TableLinkedType);
            GetByIds = LinkDM.GetType().GetMethods().Where(p => p.Name == nameof(LinkDM.GetByIds) && p.IsGenericMethod).FirstOrDefault();
            if (GetByIds != null)
            {
                GetByIds = GetByIds.MakeGenericMethod(TableLinkedType);
            }
            return result;
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
                if (int.TryParse(s, out id))
                {
                    ids.Add(id);
                }
            }

            object? objItem = GetByIds?.Invoke(LinkDM, new object[] { ids });

            SetValue(obj, objItem);
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
