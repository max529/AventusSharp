using AventusSharp.Data.Manager;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Exist;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    public class Exist
    {
        public static string PrepareSQL<X>(DatabaseExistBuilder<X> queryBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseBuilderInfo mainInfo = queryBuilder.InfoByPath[""];
            List<string> fields = new() {"COUNT(*) as nb"};
            List<string> joins = new();

            string whereTxt = BuilderTools.Where(queryBuilder.Wheres);
            
            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            string sql = "SELECT " + string.Join(",", fields)
                + " FROM `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias
                + joinTxt
                + whereTxt;


            return sql;
        }
    }
}
