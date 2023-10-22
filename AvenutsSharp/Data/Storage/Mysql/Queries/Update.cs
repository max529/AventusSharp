using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    public class Update
    {
        public static DatabaseUpdateBuilderInfo PrepareSQL<X>(DatabaseUpdateBuilder<X> updateBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseBuilderInfo mainInfo = updateBuilder.InfoByPath[""];
            List<string> fields = new();
            List<string> joins = new();
            List<TableMemberInfo> reverseMembers = new List<TableMemberInfo>();

            void loadInfo(DatabaseBuilderInfo baseInfo)
            {
                if (updateBuilder.AllFieldsUpdate)
                {
                    LoadTableFieldUpdate(baseInfo.TableInfo, baseInfo.Alias, baseInfo, updateBuilder.UpdateParamsInfo);
                }
                string lastAlias = baseInfo.Alias;
                TableInfo lastTableInfo = baseInfo.TableInfo;
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.Parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;
                    if (updateBuilder.AllFieldsUpdate)
                    {
                        LoadTableFieldUpdate(info, alias, baseInfo, updateBuilder.UpdateParamsInfo);

                    }
                    joins.Add("INNER JOIN `" + info.SqlTableName + "` " + alias + " ON " + lastAlias + "." + lastTableInfo.Primary?.SqlName + "=" + alias + "." + info.Primary?.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                }
                reverseMembers.AddRange(baseInfo.ReverseLinks);
            }

            loadInfo(mainInfo);

            foreach (KeyValuePair<string, ParamsInfo> paramInfo in updateBuilder.UpdateParamsInfo)
            {
                string name = paramInfo.Key;
                fields.Add(name + " = @" + name);
            }

            string whereTxt = BuilderTools.Where(updateBuilder.Wheres);

            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            string sql = "UPDATE `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias
                + joinTxt
                + " SET " + string.Join(",", fields)
                + whereTxt;


            KeyValuePair<TableMemberInfo?, string> pair = updateBuilder.InfoByPath[""].GetTableMemberInfoAndAlias(TypeTools.GetMemberName((IStorable i) => i.Id));
            if (pair.Key == null)
            {
                throw new Exception("Can't find Id... 0_o");
            }
            string idField = pair.Value + "." + pair.Key.SqlName;
            string querySql = "SELECT " + idField + " FROM `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias + joinTxt + whereTxt;
            return new DatabaseUpdateBuilderInfo(sql, querySql, reverseMembers);
        }

        private static void LoadTableFieldUpdate(TableInfo tableInfo, string alias, DatabaseBuilderInfo baseInfo, Dictionary<string, ParamsInfo> updateParamsInfo)
        {
            foreach (TableMemberInfo member in tableInfo.Members)
            {
                if (member.Link != TableMemberInfoLink.Multiple)
                {
                    if (member.Link != TableMemberInfoLink.Parent && member.IsUpdatable)
                    {
                        string name = alias + "." + member.SqlName;
                        updateParamsInfo.Add(name, new ParamsInfo()
                        {
                            DbType = member.SqlType,
                            Name = name,
                            TypeLvl0 = tableInfo.Type,
                            MembersList = new List<TableMemberInfo>() { member }
                        });
                    }
                }
            }

            foreach (TableMemberInfo member in tableInfo.ReverseMembers)
            {
                if (member.IsAutoCreate || member.IsAutoUpdate || member.IsAutoDelete)
                {
                    baseInfo.ReverseLinks.Add(member);
                }
            }
        }

    }
}
