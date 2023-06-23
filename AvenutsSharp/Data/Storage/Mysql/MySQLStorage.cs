using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Data.Storage.Mysql.Action;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql
{
    public class MySQLStorage : DefaultStorage<MySQLStorage>
    {
        public MySQLStorage(StorageCredentials info) : base(info)
        {
        }

        protected override DbConnection getConnection()
        {
            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
            builder.Server = host;
            builder.UserID = username;
            builder.Password = password;
            builder.Database = database;

            connection = new MySqlConnection(builder.ConnectionString);
            return connection;
        }

        public override ResultWithError<bool> ConnetWithError()
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
            try
            {
                IsConnectedOneTime = true;
                connection = getConnection();
                connection.Open();
                if (!keepConnectionOpen)
                {
                    connection.Close();
                }
                IsConnectedOneTime = true;
                result.Result = true;
            }
            catch (Exception e)
            {
                if (e is MySqlException exception)
                {
                    if (exception.Number == 1049) // missing database
                    {
                        try
                        {
                            MySqlConnectionStringBuilder builder = new MySqlConnectionStringBuilder();
                            builder.Server = host;
                            builder.UserID = username;
                            builder.Password = password;
                            connection = new MySqlConnection(builder.ConnectionString);
                            connection.Open();
                            Execute("CREATE DATABASE " + database + ";");
                            connection.Close();


                            MySqlConnectionStringBuilder builderFull = new MySqlConnectionStringBuilder();
                            builderFull.Server = host;
                            builderFull.UserID = username;
                            builderFull.Password = password;
                            builderFull.Database = database;
                            connection = new MySqlConnection(builder.ConnectionString);
                            connection.Open();
                            if (!keepConnectionOpen)
                            {
                                connection.Close();
                            }
                            IsConnectedOneTime = true;
                            result.Result = true;
                        }
                        catch (Exception e2)
                        {
                            result.Errors.Add(new DataError(DataErrorCode.UnknowError, e2));
                        }
                    }
                    else
                    {
                        result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                }
            }

            return result;
        }

        public override ResultWithError<DbCommand> CreateCmd(string sql)
        {
            ResultWithError<DbCommand> result = new ResultWithError<DbCommand>();
            MySqlConnection? mySqlConnection = (MySqlConnection?)connection;
            if (mySqlConnection != null)
            {
                MySqlCommand command = mySqlConnection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = sql;
                result.Result = command;
            }
            else
            {
                result.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
            }
            return result;
        }
        public override DbParameter GetDbParameter()
        {
            return new MySqlParameter();
        }

        internal override StorageAction<MySQLStorage> defineActions()
        {
            return new MySQLAction(this);
        }

        public override ResultWithError<bool> ResetStorage()
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
            string sql = "SELECT concat('DROP TABLE IF EXISTS `', table_name, '`;') as query FROM information_schema.tables WHERE table_schema = '" + this.database + "'; ";
            StorageQueryResult queryResult = Query(sql);
            if (!queryResult.Success)
            {
                result.Errors.AddRange(queryResult.Errors);
                return result;
            }

            string dropAllCmd = "SET FOREIGN_KEY_CHECKS = 0;";
            foreach (Dictionary<string, string> line in queryResult.Result)
            {
                dropAllCmd += line["query"];
            }
            dropAllCmd += "SET FOREIGN_KEY_CHECKS = 1;";

            StorageExecutResult executeResult = Execute(dropAllCmd);
            if (!executeResult.Success)
            {
                result.Errors.AddRange(executeResult.Errors);
                return result;
            }

            result.Result = true;
            return result;
        }

        public override void BuildQueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder)
        {
            DatabaseQueryBuilderInfo mainInfo = queryBuilder.infoByPath[""];
            List<string> fields = new List<string>();
            List<string> joins = new List<string>();

            Action<DatabaseQueryBuilderInfo> loadInfo = null;
            loadInfo = (baseInfo) =>
            {

                if (queryBuilder.allMembers)
                {
                    loadTableField(baseInfo.tableInfo, baseInfo.alias, fields);
                }
                string lastAlias = baseInfo.alias;
                TableInfo lastTableInfo = baseInfo.tableInfo;
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;
                    if (queryBuilder.allMembers)
                    {
                        loadTableField(info, alias, fields);
                    }
                    joins.Add("INNER JOIN " + info.SqlTableName + " " + alias + " ON " + lastAlias + "." + lastTableInfo.primary.SqlName + "=" + alias + "." + info.primary.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                }

                Action<List<DatabaseQueryBuilderInfoChild>, string, string> loadChild = null;
                loadChild = delegate (List<DatabaseQueryBuilderInfoChild> children, string parentAlias, string parentPrimName)
                {
                    foreach (DatabaseQueryBuilderInfoChild child in children)
                    {
                        string alias = child.alias;
                        string primName = child.tableInfo.primary.SqlName;
                        if (queryBuilder.allMembers)
                        {
                            loadTableField(child.tableInfo, alias, fields);
                        }
                        joins.Add("LEFT OUTER JOIN " + child.tableInfo.SqlTableName + " " + child.alias + " ON " + parentAlias + "." + parentPrimName + "=" + alias + "." + primName);
                        loadChild(child.children, alias, primName);
                    }
                };
                loadChild(baseInfo.children, baseInfo.alias, baseInfo.tableInfo.primary.SqlName);

                if (!queryBuilder.allMembers)
                {
                    foreach (KeyValuePair<TableMemberInfo, string> member in baseInfo.members)
                    {
                        string alias = member.Value;
                        fields.Add(alias + "." + member.Key.SqlName + " `" + alias + "*" + member.Key.SqlName + "`");
                    }
                }


                foreach (KeyValuePair<DatabaseQueryBuilderInfo, TableMemberInfo> linkInfo in baseInfo.links)
                {

                    joins.Add("LEFT OUTER JOIN " + linkInfo.Key.tableInfo.SqlTableName + " " + linkInfo.Key.alias + " ON " + baseInfo.alias + "." + linkInfo.Value.SqlName + "=" + linkInfo.Key.alias + "." + linkInfo.Key.tableInfo.primary.SqlName);
                    loadInfo(linkInfo.Key);
                }
            };
            loadInfo(mainInfo);

            string whereTxt = "";
            if (queryBuilder.wheres != null)
            {
                Func<WhereQueryGroup, string, string> buildWhere = null;
                buildWhere = (whereGroup, whereTxt) =>
                {
                    whereTxt += "(";
                    string subQuery = "";
                    IWhereQueryGroup? lastGroup = null;
                    foreach (IWhereQueryGroup queryGroup in whereGroup.queryGroups)
                    {
                        if (queryGroup is WhereQueryGroup childWhereGroup)
                        {
                            subQuery += buildWhere(childWhereGroup, "");
                        }
                        else if (queryGroup is WhereQueryGroupFct fctGroup)
                        {
                            subQuery += GetFctName(fctGroup.fct);
                        }
                        else if (queryGroup is WhereQueryGroupConstantNull nullConst)
                        {
                            // special case for IS and IS NOT
                            if (whereGroup.queryGroups.Count == 3)
                            {
                                WhereQueryGroupFct? fctGrp = null;
                                WhereQueryGroupField? fieldGrp = null;
                                for (int i = 0; i < whereGroup.queryGroups.Count; i++)
                                {
                                    if (whereGroup.queryGroups[i] is WhereQueryGroupFct fctGrpTemp && (fctGrpTemp.fct == WhereQueryGroupFctEnum.Equal || fctGrpTemp.fct == WhereQueryGroupFctEnum.NotEqual))
                                    {
                                        fctGrp = fctGrpTemp;
                                    }
                                    else if (whereGroup.queryGroups[i] is WhereQueryGroupField fieldGrpTemp)
                                    {
                                        fieldGrp = fieldGrpTemp;
                                    }
                                }

                                if (fctGrp != null && fieldGrp != null)
                                {
                                    string action = fctGrp.fct == WhereQueryGroupFctEnum.Equal ? " IS NULL" : " IS NOT NULL";
                                    subQuery = fieldGrp.alias + "." + fieldGrp.tableMemberInfo.SqlName + action;
                                    break;
                                }
                            }

                            subQuery += "NULL";
                        }
                        else if (queryGroup is WhereQueryGroupConstantBool boolConst)
                        {
                            subQuery += boolConst.value ? "1" : "0";
                        }
                        else if (queryGroup is WhereQueryGroupConstantString stringConst)
                        {
                            string strValue = "'" + stringConst.value + "'";
                            if (lastGroup is WhereQueryGroupFct groupFct)
                            {
                                if (groupFct.fct == WhereQueryGroupFctEnum.StartsWith)
                                {
                                    strValue = "'" + stringConst.value + "%'";
                                }
                                else if (groupFct.fct == WhereQueryGroupFctEnum.EndsWith)
                                {
                                    strValue = "'%" + stringConst.value + "'";
                                }
                                else if (groupFct.fct == WhereQueryGroupFctEnum.Contains)
                                {
                                    strValue = "'%" + stringConst.value + "%'";
                                }
                            }
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereQueryGroupConstantDateTime dateTimeConst)
                        {
                            subQuery += "'" + dateTimeConst.value.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        }
                        else if (queryGroup is WhereQueryGroupConstantOther otherConst)
                        {
                            subQuery += otherConst.value;
                        }
                        else if (queryGroup is WhereQueryGroupField fieldGrp)
                        {
                            subQuery += fieldGrp.alias + "." + fieldGrp.tableMemberInfo.SqlName;
                        }
                        lastGroup = queryGroup;
                    }
                    whereTxt += subQuery;
                    whereTxt += ")";
                    return whereTxt;
                };
                foreach (WhereQueryGroup whereGroup in queryBuilder.wheres)
                {
                    whereTxt += buildWhere(whereGroup, whereTxt);
                }
                if (whereTxt.Length > 1)
                {
                    whereTxt = " WHERE " + whereTxt;
                }
            }

            string joinTxt = string.Join(" ", joins);
            if (joinTxt.Length > 1)
            {
                joinTxt = " " + joinTxt;
            }

            string sql = "SELECT " + string.Join(",", fields)
                + " FROM " + mainInfo.tableInfo.SqlTableName + " " + mainInfo.alias
                + joinTxt
                + whereTxt;


            Console.WriteLine(sql);
        }

        private string GetFctName(WhereQueryGroupFctEnum fctEnum)
        {
            switch (fctEnum)
            {
                case WhereQueryGroupFctEnum.Add:
                    return " + ";
                case WhereQueryGroupFctEnum.And:
                    return " AND ";
                case WhereQueryGroupFctEnum.Contains:
                case WhereQueryGroupFctEnum.StartsWith:
                case WhereQueryGroupFctEnum.EndsWith:
                    return " LIKE ";
                case WhereQueryGroupFctEnum.Divide:
                    return " / ";
                case WhereQueryGroupFctEnum.Equal:
                    return " = ";
                case WhereQueryGroupFctEnum.GreaterThan:
                    return " > ";
                case WhereQueryGroupFctEnum.GreaterThanOrEqual:
                    return " >= ";
                case WhereQueryGroupFctEnum.LessThan:
                    return " < ";
                case WhereQueryGroupFctEnum.LessThanOrEqual:
                    return " <= ";
                case WhereQueryGroupFctEnum.Multiply:
                    return " * ";
                case WhereQueryGroupFctEnum.Not:
                    return " NOT ";
                case WhereQueryGroupFctEnum.NotEqual:
                    return " <> ";
                case WhereQueryGroupFctEnum.Or:
                    return " OR ";
                case WhereQueryGroupFctEnum.Subtract:
                    return " - ";
            }
            return "";
        }

        private void loadTableField(TableInfo tableInfo, string alias, List<string> fields)
        {
            foreach (TableMemberInfo member in tableInfo.members)
            {
                if (member.link != TableMemberInfoLink.Multiple)
                {
                    if (member.link != TableMemberInfoLink.Parent)
                    {
                        fields.Add(alias + "." + member.SqlName + " `" + alias + "*" + member.SqlName + "`");
                    }
                }
            }
        }
    }
}
