using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Query
{
    internal class WhereQueryInfo
    {
        public string sql { get; set; } = "";
        public List<TableMemberInfo> memberInfos { get; set; } = new List<TableMemberInfo>();
    }
    internal class Where
    {
        private static Dictionary<TableInfo, WhereQueryInfo> queriesInfo = new Dictionary<TableInfo, WhereQueryInfo>();

        private class WhereQueryInfoTemp
        {
            public List<string> fields = new List<string>();
            public List<string> join = new List<string>();
            public List<TableMemberInfo> memberInfos = new List<TableMemberInfo>();

        }

        public static WhereQueryInfo GetQueryInfo(TableInfo tableInfo, BinaryExpression func, MySQLStorage storage)
        {
            if (!queriesInfo.ContainsKey(tableInfo))
            {
                createQueryInfo(tableInfo, func, storage);
            }
            return queriesInfo[tableInfo];
        }

        private static void createQueryInfo(TableInfo table, BinaryExpression func, MySQLStorage storage)
        {
            generateSQLWhere(func, table.Type);
            Console.WriteLine("in");
        }

        public static string generateSQLWhere(Expression e, Type type)
        {
            string where = "";
            Console.WriteLine(e.NodeType);
            if (e is BinaryExpression binary)
            {
                if (operators.ContainsKey(e.NodeType))
                {
                    string _operator = operators[e.NodeType];
                    generateSQLWhere(binary.Left, type);
                    generateSQLWhere(binary.Right, type);
                }
                
            }
            
            return where;
        }

        private static Dictionary<ExpressionType, string> operators = new Dictionary<ExpressionType, string>() {
            { ExpressionType.Equal, "=" },
            { ExpressionType.And, " and " },
            { ExpressionType.AndAlso, " and " },
            { ExpressionType.Or, " or " },
            { ExpressionType.OrElse, " or " },
            { ExpressionType.GreaterThan, " > " },
            { ExpressionType.GreaterThanOrEqual, " >= " },
            { ExpressionType.LessThan, " < " },
            { ExpressionType.LessThanOrEqual, " <= " },
        };

       
    }
}
