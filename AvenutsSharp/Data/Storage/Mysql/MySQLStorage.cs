using AventusSharp.Data.Manager;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Data.Storage.Mysql.Action;
using AventusSharp.Tools;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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

        #region query
        private string PrepareSQLForQuery<X>(DatabaseQueryBuilder<X> queryBuilder)
        {
            DatabaseBuilderInfo mainInfo = queryBuilder.infoByPath[""];
            List<string> fields = new List<string>();
            List<string> joins = new List<string>();

            Action<DatabaseBuilderInfo> loadInfo = null;
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

                Action<List<DatabaseBuilderInfoChild>, string, string> loadChild = null;
                loadChild = delegate (List<DatabaseBuilderInfoChild> children, string parentAlias, string parentPrimName)
                {
                    foreach (DatabaseBuilderInfoChild child in children)
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


                foreach (KeyValuePair<TableMemberInfo, DatabaseBuilderInfo> linkInfo in baseInfo.links)
                {
                    TableMemberInfo tableMemberInfo = linkInfo.Key;
                    DatabaseBuilderInfo databaseQueryBuilderInfo = linkInfo.Value;
                    joins.Add("LEFT OUTER JOIN " + databaseQueryBuilderInfo.tableInfo.SqlTableName + " " + databaseQueryBuilderInfo.alias + " ON " + baseInfo.alias + "." + tableMemberInfo.SqlName + "=" + databaseQueryBuilderInfo.alias + "." + databaseQueryBuilderInfo.tableInfo.primary.SqlName);
                    loadInfo(databaseQueryBuilderInfo);
                }
            };
            loadInfo(mainInfo);

            string whereTxt = "";
            if (queryBuilder.wheres != null)
            {
                Func<WhereGroup, string, string> buildWhere = null;
                buildWhere = (whereGroup, whereTxt) =>
                {
                    whereTxt += "(";
                    string subQuery = "";
                    IWhereGroup? lastGroup = null;
                    foreach (IWhereGroup queryGroup in whereGroup.groups)
                    {
                        if (queryGroup is WhereGroup childWhereGroup)
                        {
                            subQuery += buildWhere(childWhereGroup, "");
                        }
                        else if (queryGroup is WhereGroupFct fctGroup)
                        {
                            subQuery += GetFctName(fctGroup.fct);
                        }
                        else if (queryGroup is WhereGroupConstantNull nullConst)
                        {
                            // special case for IS and IS NOT
                            if (whereGroup.groups.Count == 3)
                            {
                                WhereGroupFct? fctGrp = null;
                                WhereGroupField? fieldGrp = null;
                                for (int i = 0; i < whereGroup.groups.Count; i++)
                                {
                                    if (whereGroup.groups[i] is WhereGroupFct fctGrpTemp && (fctGrpTemp.fct == WhereGroupFctEnum.Equal || fctGrpTemp.fct == WhereGroupFctEnum.NotEqual))
                                    {
                                        fctGrp = fctGrpTemp;
                                    }
                                    else if (whereGroup.groups[i] is WhereGroupField fieldGrpTemp)
                                    {
                                        fieldGrp = fieldGrpTemp;
                                    }
                                }

                                if (fctGrp != null && fieldGrp != null)
                                {
                                    string action = fctGrp.fct == WhereGroupFctEnum.Equal ? " IS NULL" : " IS NOT NULL";
                                    subQuery = fieldGrp.alias + "." + fieldGrp.tableMemberInfo.SqlName + action;
                                    break;
                                }
                            }

                            subQuery += "NULL";
                        }
                        else if (queryGroup is WhereGroupConstantBool boolConst)
                        {
                            subQuery += boolConst.value ? "1" : "0";
                        }
                        else if (queryGroup is WhereGroupConstantString stringConst)
                        {
                            string strValue = "'" + stringConst.value + "'";
                            if (lastGroup is WhereGroupFct groupFct)
                            {
                                if (groupFct.fct == WhereGroupFctEnum.StartsWith)
                                {
                                    strValue = "'" + stringConst.value + "%'";
                                }
                                else if (groupFct.fct == WhereGroupFctEnum.EndsWith)
                                {
                                    strValue = "'%" + stringConst.value + "'";
                                }
                                else if (groupFct.fct == WhereGroupFctEnum.Contains)
                                {
                                    strValue = "'%" + stringConst.value + "%'";
                                }
                            }
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereGroupConstantDateTime dateTimeConst)
                        {
                            subQuery += "'" + dateTimeConst.value.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        }
                        else if (queryGroup is WhereGroupConstantOther otherConst)
                        {
                            subQuery += otherConst.value;
                        }
                        else if (queryGroup is WhereGroupConstantParameter paramConst)
                        {
                            string strValue = "@" + paramConst.value;
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereGroupField fieldGrp)
                        {
                            subQuery += fieldGrp.alias + "." + fieldGrp.tableMemberInfo.SqlName;
                        }
                        lastGroup = queryGroup;
                    }
                    whereTxt += subQuery;
                    whereTxt += ")";
                    return whereTxt;
                };
                foreach (WhereGroup whereGroup in queryBuilder.wheres)
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
            return sql;
        }
        public override ResultWithError<List<X>> QueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder)
        {
            ResultWithError<List<X>> result = new ResultWithError<List<X>>();

            string sql = "";
            if (queryBuilder.sql != null)
            {
                sql = queryBuilder.sql;
            }
            else
            {
                sql = PrepareSQLForQuery(queryBuilder);
                queryBuilder.sql = sql;
            }

            ResultWithError<DbCommand> cmdResult = CreateCmd(sql);
            result.Errors.AddRange(cmdResult.Errors);
            if (!result.Success || cmdResult.Result == null)
            {
                return result;
            }
            DbCommand cmd = cmdResult.Result;
            Dictionary<string, object?> parametersValue = new Dictionary<string, object?>();
            foreach (KeyValuePair<string, ParamsInfo> parameterInfo in queryBuilder.whereParamsInfo)
            {
                DbParameter parameter = GetDbParameter();
                parameter.ParameterName = "@" + parameterInfo.Key;
                parameter.DbType = parameterInfo.Value.dbType;
                cmd.Parameters.Add(parameter);
                parametersValue["@"+parameterInfo.Key] = TransformValueForFct(parameterInfo.Value);
            }

            StorageQueryResult queryResult = Query(cmd, new List<Dictionary<string, object?>>() { parametersValue });
            cmd.Dispose();

            result.Errors.AddRange(queryResult.Errors);
            if (queryResult.Success)
            {
                result.Result = new List<X>();
                DatabaseBuilderInfo baseInfo = queryBuilder.infoByPath[""];
                
                for (int i = 0; i < queryResult.Result.Count; i++)
                {
                    Dictionary<string, string> itemFields = queryResult.Result[i];
                    object o = CreateObject(baseInfo, itemFields, queryBuilder.allMembers);
                    if(o is X oCasted)
                    {
                        result.Result.Add(oCasted);
                    }
                }
            }

            return result;
        }
        private object CreateObject(DatabaseBuilderInfo info, Dictionary<string, string> itemFields, bool allMembers)
        {
            string rootAlias = info.alias;
            TableInfo rootTableInfo = info.tableInfo;

            while (rootTableInfo.Parent != null)
            {
                rootTableInfo = rootTableInfo.Parent;
            }
            if (rootTableInfo != info.tableInfo)
            {
                rootAlias = info.parents[rootTableInfo];
            }

            object o;
            if (info.tableInfo.IsAbstract)
            {
                string fieldTypeName = rootAlias + "." + TableInfo.TypeIdentifierName;
                if (!itemFields.ContainsKey(fieldTypeName))
                {
                    throw new DataError(DataErrorCode.NoTypeIdentifierFoundInsideQuery, "Can't find the field " + TableInfo.TypeIdentifierName).GetException();
                }

                Type? typeToCreate = Type.GetType(itemFields[fieldTypeName]);
                if (typeToCreate == null)
                {
                    throw new DataError(DataErrorCode.WrongType, "Can't find the type " + itemFields[fieldTypeName]).GetException();
                }

                o = TypeTools.CreateNewObj(typeToCreate);
            }
            else
            {
                o = TypeTools.CreateNewObj(rootTableInfo.Type);
            }

            // TODO : optimize this method by storing needed values
            TableInfo? childest = info.tableInfo;
            if (info.tableInfo.Type != o.GetType())
            {
                DatabaseBuilderInfoChild? childTemp = info.children.Find(c => c.tableInfo.Type == o.GetType());
                childest = childTemp?.tableInfo;
            }
            while (childest != null)
            {
                string alias = "";
                DatabaseBuilderInfoChild? tempChild = info.children.Find(c => c.tableInfo == childest);
                if (tempChild != null)
                {
                    alias = tempChild.alias;
                }
                else if (childest == info.tableInfo)
                {
                    alias = info.alias;
                }
                else if (info.parents.ContainsKey(childest))
                {
                    alias = info.parents[childest];
                }
                else
                {
                    throw new Exception("Can't found what I wnat");
                }

                foreach (TableMemberInfo memberInfo in childest.members)
                {
                    if(!allMembers && !info.members.ContainsKey(memberInfo))
                    {
                        continue;
                    }
                    string key = alias + "*" + memberInfo.SqlName;
                    if (itemFields.ContainsKey(key))
                    {
                        if (memberInfo.link == TableMemberInfoLink.None)
                        {
                            memberInfo.SetSqlValue(o, itemFields[key]);
                        }
                        else if (memberInfo.link == TableMemberInfoLink.Simple)
                        {
                            if (itemFields[key] != "")
                            {
                                object oTemp = CreateObject(info.links[memberInfo], itemFields, allMembers);
                                memberInfo.SetValue(o, oTemp);
                            }
                        }
                    }
                }
                childest = childest.Parent;
            }
            return o;
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

        #endregion

        #region update
        private string PrepareSQLForUpdate<X>(DatabaseUpdateBuilder<X> updateBuilder)
        {
            DatabaseBuilderInfo mainInfo = updateBuilder.infoByPath[""];
            List<string> fields = new List<string>();
            List<string> joins = new List<string>();

            Action<DatabaseBuilderInfo> loadInfo = null;
            loadInfo = (baseInfo) =>
            {

                string lastAlias = baseInfo.alias;
                TableInfo lastTableInfo = baseInfo.tableInfo;
                foreach (KeyValuePair<TableInfo, string> parentLink in baseInfo.parents)
                {
                    string alias = parentLink.Value;
                    TableInfo info = parentLink.Key;
                    joins.Add("INNER JOIN " + info.SqlTableName + " " + alias + " ON " + lastAlias + "." + lastTableInfo.primary.SqlName + "=" + alias + "." + info.primary.SqlName);
                    lastAlias = alias;
                    lastTableInfo = info;
                }

                Action<List<DatabaseBuilderInfoChild>, string, string> loadChild = null;
                loadChild = delegate (List<DatabaseBuilderInfoChild> children, string parentAlias, string parentPrimName)
                {
                    foreach (DatabaseBuilderInfoChild child in children)
                    {
                        string alias = child.alias;
                        string primName = child.tableInfo.primary.SqlName;
                        joins.Add("LEFT OUTER JOIN " + child.tableInfo.SqlTableName + " " + child.alias + " ON " + parentAlias + "." + parentPrimName + "=" + alias + "." + primName);
                        loadChild(child.children, alias, primName);
                    }
                };
                loadChild(baseInfo.children, baseInfo.alias, baseInfo.tableInfo.primary.SqlName);

                foreach (KeyValuePair<TableMemberInfo, DatabaseBuilderInfo> linkInfo in baseInfo.links)
                {
                    TableMemberInfo tableMemberInfo = linkInfo.Key;
                    DatabaseBuilderInfo databaseQueryBuilderInfo = linkInfo.Value;
                    joins.Add("LEFT OUTER JOIN " + databaseQueryBuilderInfo.tableInfo.SqlTableName + " " + databaseQueryBuilderInfo.alias + " ON " + baseInfo.alias + "." + tableMemberInfo.SqlName + "=" + databaseQueryBuilderInfo.alias + "." + databaseQueryBuilderInfo.tableInfo.primary.SqlName);
                    loadInfo(databaseQueryBuilderInfo);
                }
            };
            loadInfo(mainInfo);

            foreach (KeyValuePair<string, ParamsInfo> paramInfo in updateBuilder.updateParamsInfo)
            {
                string name = paramInfo.Key;
                DbType dbType = paramInfo.Value.dbType;
                List<DbType> escapedTypes = new List<DbType>() { DbType.String, DbType.DateTime };
                bool mustBeEscaped = escapedTypes.Contains(dbType);
                if (mustBeEscaped)
                {
                    fields.Add(name + " = '@" + name+"'");
                }
                else
                {
                    fields.Add(name + " = @" + name);

                }
            }

            string whereTxt = "";
            if (updateBuilder.wheres != null)
            {
                Func<WhereGroup, string, string> buildWhere = null;
                buildWhere = (whereGroup, whereTxt) =>
                {
                    whereTxt += "(";
                    string subQuery = "";
                    IWhereGroup? lastGroup = null;
                    foreach (IWhereGroup queryGroup in whereGroup.groups)
                    {
                        if (queryGroup is WhereGroup childWhereGroup)
                        {
                            subQuery += buildWhere(childWhereGroup, "");
                        }
                        else if (queryGroup is WhereGroupFct fctGroup)
                        {
                            subQuery += GetFctName(fctGroup.fct);
                        }
                        else if (queryGroup is WhereGroupConstantNull nullConst)
                        {
                            // special case for IS and IS NOT
                            if (whereGroup.groups.Count == 3)
                            {
                                WhereGroupFct? fctGrp = null;
                                WhereGroupField? fieldGrp = null;
                                for (int i = 0; i < whereGroup.groups.Count; i++)
                                {
                                    if (whereGroup.groups[i] is WhereGroupFct fctGrpTemp && (fctGrpTemp.fct == WhereGroupFctEnum.Equal || fctGrpTemp.fct == WhereGroupFctEnum.NotEqual))
                                    {
                                        fctGrp = fctGrpTemp;
                                    }
                                    else if (whereGroup.groups[i] is WhereGroupField fieldGrpTemp)
                                    {
                                        fieldGrp = fieldGrpTemp;
                                    }
                                }

                                if (fctGrp != null && fieldGrp != null)
                                {
                                    string action = fctGrp.fct == WhereGroupFctEnum.Equal ? " IS NULL" : " IS NOT NULL";
                                    subQuery = fieldGrp.alias + "." + fieldGrp.tableMemberInfo.SqlName + action;
                                    break;
                                }
                            }

                            subQuery += "NULL";
                        }
                        else if (queryGroup is WhereGroupConstantBool boolConst)
                        {
                            subQuery += boolConst.value ? "1" : "0";
                        }
                        else if (queryGroup is WhereGroupConstantString stringConst)
                        {
                            string strValue = "'" + stringConst.value + "'";
                            if (lastGroup is WhereGroupFct groupFct)
                            {
                                if (groupFct.fct == WhereGroupFctEnum.StartsWith)
                                {
                                    strValue = "'" + stringConst.value + "%'";
                                }
                                else if (groupFct.fct == WhereGroupFctEnum.EndsWith)
                                {
                                    strValue = "'%" + stringConst.value + "'";
                                }
                                else if (groupFct.fct == WhereGroupFctEnum.Contains)
                                {
                                    strValue = "'%" + stringConst.value + "%'";
                                }
                            }
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereGroupConstantDateTime dateTimeConst)
                        {
                            subQuery += "'" + dateTimeConst.value.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                        }
                        else if (queryGroup is WhereGroupConstantOther otherConst)
                        {
                            subQuery += otherConst.value;
                        }
                        else if (queryGroup is WhereGroupConstantParameter paramConst)
                        {
                            string strValue = "@" + paramConst.value;
                            if (lastGroup is WhereGroupFct groupFct)
                            {
                                if (groupFct.fct == WhereGroupFctEnum.StartsWith)
                                {
                                    strValue = "@" + paramConst.value + "%";
                                }
                                else if (groupFct.fct == WhereGroupFctEnum.EndsWith)
                                {
                                    strValue = "%@" + paramConst.value;
                                }
                                else if (groupFct.fct == WhereGroupFctEnum.Contains)
                                {
                                    strValue = "%@" + paramConst.value + "%";
                                }
                            }
                            subQuery += strValue;
                        }
                        else if (queryGroup is WhereGroupField fieldGrp)
                        {
                            subQuery += fieldGrp.alias + "." + fieldGrp.tableMemberInfo.SqlName;
                        }
                        lastGroup = queryGroup;
                    }
                    whereTxt += subQuery;
                    whereTxt += ")";
                    return whereTxt;
                };
                foreach (WhereGroup whereGroup in updateBuilder.wheres)
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

            string sql = "UPDATE " + mainInfo.tableInfo.SqlTableName + " " + mainInfo.alias
                + joinTxt
                +" SET "+ string.Join(",", fields)
                + whereTxt;


            Console.WriteLine(sql);
            return sql;
        }
        public override ResultWithError<X> UpdateFromBuilder<X>(DatabaseUpdateBuilder<X> updateBuilder, X item)
        {
            ResultWithError<X> result = new ResultWithError<X>();
            if(item == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "Please provide an item to use for update"));
                return result;
            }
            string sql = "";
            if (updateBuilder.sql != null)
            {
                sql = updateBuilder.sql;
            }
            else
            {
                sql = PrepareSQLForUpdate(updateBuilder);
                updateBuilder.sql = sql;
            }

            ResultWithError<DbCommand> cmdResult = CreateCmd(sql);
            result.Errors.AddRange(cmdResult.Errors);
            if (!result.Success || cmdResult.Result == null)
            {
                return result;
            }
            DbCommand cmd = cmdResult.Result;
            Dictionary<string, object?> parametersValue = new Dictionary<string, object?>();
            foreach (KeyValuePair<string, ParamsInfo> parameterInfo in updateBuilder.whereParamsInfo)
            {
                DbParameter parameter = GetDbParameter();
                parameter.ParameterName = parameterInfo.Key;
                parameter.DbType = parameterInfo.Value.dbType;
                cmd.Parameters.Add(parameter);
                parametersValue[parameterInfo.Key] = parameterInfo.Value.value;
            }
            foreach (KeyValuePair<string, ParamsInfo> parameterInfo in updateBuilder.updateParamsInfo)
            {
                DbParameter parameter = GetDbParameter();
                parameter.ParameterName = parameterInfo.Key;
                parameter.DbType = parameterInfo.Value.dbType;
                cmd.Parameters.Add(parameter);
                parameterInfo.Value.typeLvl0 = item.GetType();
                parameterInfo.Value.SetValue(item);
                parametersValue[parameterInfo.Key] = parameterInfo.Value.value;
            }

            StorageQueryResult queryResult = Query(cmd, new List<Dictionary<string, object?>>() { parametersValue });
            cmd.Dispose();

            return result;
        }
        #endregion

        private string GetFctName(WhereGroupFctEnum fctEnum)
        {
            switch (fctEnum)
            {
                case WhereGroupFctEnum.Add:
                    return " + ";
                case WhereGroupFctEnum.And:
                    return " AND ";
                case WhereGroupFctEnum.Contains:
                case WhereGroupFctEnum.StartsWith:
                case WhereGroupFctEnum.EndsWith:
                    return " LIKE ";
                case WhereGroupFctEnum.Divide:
                    return " / ";
                case WhereGroupFctEnum.Equal:
                    return " = ";
                case WhereGroupFctEnum.GreaterThan:
                    return " > ";
                case WhereGroupFctEnum.GreaterThanOrEqual:
                    return " >= ";
                case WhereGroupFctEnum.LessThan:
                    return " < ";
                case WhereGroupFctEnum.LessThanOrEqual:
                    return " <= ";
                case WhereGroupFctEnum.Multiply:
                    return " * ";
                case WhereGroupFctEnum.Not:
                    return " NOT ";
                case WhereGroupFctEnum.NotEqual:
                    return " <> ";
                case WhereGroupFctEnum.Or:
                    return " OR ";
                case WhereGroupFctEnum.Subtract:
                    return " - ";
            }
            return "";
        }

        private object? TransformValueForFct(ParamsInfo paramsInfo)
        {
            if (paramsInfo.value is string casted)
            {
                if (paramsInfo.fctMethodCall == WhereGroupFctEnum.StartsWith)
                {
                    return casted + "%";
                }
                if (paramsInfo.fctMethodCall == WhereGroupFctEnum.EndsWith)
                {
                    return "%" + casted;
                }
                if (paramsInfo.fctMethodCall == WhereGroupFctEnum.Contains)
                {
                    return "%" + casted + "%";
                }
            }
            return paramsInfo.value;
        }
        
    }
}
