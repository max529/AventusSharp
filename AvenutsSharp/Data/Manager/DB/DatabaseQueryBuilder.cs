using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager.DB
{
    public class DatabaseQueryBuilder<T> : QueryBuilder<T>
    {
        protected IStorage Storage { get; private set; }

        private TableInfo? _tableInfo;

        public Dictionary<string, TableMemberInfo> members = new Dictionary<string, TableMemberInfo>();
        public Dictionary<string, TableMemberInfo> includes = new Dictionary<string, TableMemberInfo>();

        public DatabaseQueryBuilder(QueryBuildType type, IStorage storage) : base(type)
        {
            this.Storage = storage;
        }

        private TableInfo GetTableInfo()
        {
            if(_tableInfo != null)
            {
                return _tableInfo;
            }
            TableInfo? tableInfo = Storage.GetTableInfo(typeof(T));
            if(tableInfo != null)
            {
                _tableInfo = tableInfo;
                return tableInfo;
            }
            throw new Exception();
        }

        public override void Execute()
        {
            throw new NotImplementedException();
        }

        public override List<T> Query()
        {
            Storage.BuildQueryFromBuilder(this);
            return new List<T>();
        }

        public override QueryBuilder<T> Where(Expression<Func<T, bool>> func)
        {

            return this;
        }

        public override QueryBuilder<T> Field(Expression<Func<T, object>> memberExpression)
        {
            if (memberExpression is MemberExpression mExpression)
            {
                string memberName = mExpression.Member.Name;
                TableInfo table = GetTableInfo();
                TableMemberInfo? member = table.members.Find(m => m.Name == memberName);
                if(member == null)
                {
                    throw new Exception();
                }
                if (!members.ContainsKey(memberName))
                {
                    members.Add(memberName, member);
                }
                return this;
            }
            throw new Exception();
        }

        public override QueryBuilder<T> Include(Expression<Func<T, IStorable>> memberExpression)
        {
            if (memberExpression is MemberExpression mExpression)
            {
                string memberName = mExpression.Member.Name;
                TableInfo table = GetTableInfo();
                TableMemberInfo? member = table.members.Find(m => m.Name == memberName);
                if (member == null)
                {
                    throw new Exception();
                }
                if(member.link != TableMemberInfoLink.Simple && member.link != TableMemberInfoLink.Multiple)
                {
                    throw new Exception();
                }
                if (!includes.ContainsKey(memberName))
                {
                    includes.Add(memberName, member);
                }
                return this;
            }
            throw new Exception();
        }
    }
}
