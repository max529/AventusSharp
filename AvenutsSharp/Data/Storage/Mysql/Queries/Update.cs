using AventusSharp.Data.Manager;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Builders;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.TableMember;
using AventusSharp.Tools;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace AventusSharp.Data.Storage.Mysql.Queries
{
    public class Update
    {
        public static DatabaseUpdateBuilderInfo PrepareSQL<X>(DatabaseUpdateBuilder<X> updateBuilder, MySQLStorage storage) where X : IStorable
        {
            DatabaseUpdateBuilderInfo result = new DatabaseUpdateBuilderInfo("", "");
            DatabaseBuilderInfo mainInfo = updateBuilder.InfoByPath[""];
            List<string> fields = new();
            List<string> joins = new();

            void loadInfo(DatabaseBuilderInfo baseInfo)
            {
                if (updateBuilder.AllFieldsUpdate)
                {
                    storage.LoadAllTableFieldsUpdate<X>(baseInfo.TableInfo, baseInfo.Alias, baseInfo);
                }

                string lastAlias = baseInfo.Alias;
                TableInfo lastTableInfo = baseInfo.TableInfo;

                foreach (KeyValuePair<TableMemberInfoSql, DatabaseBuilderInfoMember> member in baseInfo.Members)
                {
                    if(member.Key is ITableMemberInfoSqlLink)
                    {
                        if (member.Key.IsAutoCreate || member.Key.IsAutoUpdate || member.Key.IsAutoDelete)
                        {
                            result.ToCheckBefore.Add(member.Key);
                        }
                    }
                    if(member.Key is ITableMemberInfoSqlWritable writable)
                    {
                        string alias = member.Value.Alias;
                        string name = alias + "." + member.Key.SqlName;
                        updateBuilder.UpdateParamsInfo.Add(name, new ParamsInfo()
                        {
                            DbType = writable.SqlType,
                            Name = name,
                            TypeLvl0 = baseInfo.TableInfo.Type,
                            MembersList = new List<TableMemberInfoSql>() { member.Key }
                        });
                        fields.Add(name + " = @" + name);
                    }
                    
                }
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
                result.ReverseMembers.AddRange(baseInfo.ReverseLinks);
            }

            loadInfo(mainInfo);

            string whereTxt = BuilderTools.Where(updateBuilder.Wheres);

            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            result.UpdateSql = "UPDATE `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias
                + joinTxt
                + " SET " + string.Join(",", fields)
                + whereTxt;


            KeyValuePair<TableMemberInfoSql?, string> pair = updateBuilder.InfoByPath[""].GetTableMemberInfoAndAlias(TypeTools.GetMemberName((IStorable i) => i.Id));
            if (pair.Key == null)
            {
                throw new Exception("Can't find Id... 0_o");
            }
            string idField = pair.Value + "." + pair.Key.SqlName;
            result.QuerySql = "SELECT " + idField + " FROM `" + mainInfo.TableInfo.SqlTableName + "` " + mainInfo.Alias + joinTxt + whereTxt;
            return result;
        }

        private static void LoadTableFieldUpdate(TableInfo tableInfo, string alias, DatabaseBuilderInfo baseInfo, Dictionary<string, ParamsInfo> updateParamsInfo)
        {
            foreach (TableMemberInfoSql member in tableInfo.Members)
            {
                if(!member.IsUpdatable)
                {
                    return;
                }

                if(member is ITableMemberInfoSqlWritable memberLink)
                {
                    string name = alias + "." + member.SqlName;
                    updateParamsInfo.Add(name, new ParamsInfo()
                    {
                        DbType = memberLink.SqlType,
                        Name = name,
                        TypeLvl0 = tableInfo.Type,
                        MembersList = new List<TableMemberInfoSql>() { member }
                    });
                }
            }

            foreach (TableReverseMemberInfo member in tableInfo.ReverseMembers)
            {
                if (member.IsAutoCreate || member.IsAutoUpdate || member.IsAutoDelete)
                {
                    baseInfo.ReverseLinks.Add(member);
                }
            }
        }

    }
}
