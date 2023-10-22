using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Create;
using AventusSharp.Data.Manager.DB.Delete;
using AventusSharp.Data.Manager.DB.Exist;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace AventusSharp.Data.Storage.Mysql
{
    public class MySQLStorage : DefaultDBStorage<MySQLStorage>
    {
        public MySQLStorage(StorageCredentials info) : base(info)
        {
        }

        protected override DbConnection GetConnection()
        {
            MySqlConnectionStringBuilder builder = new()
            {
                Server = host,
                UserID = username,
                Password = password,
                Database = database
            };

            connection = new MySqlConnection(builder.ConnectionString);
            return connection;
        }

        public override VoidWithDataError ConnectWithError()
        {
            VoidWithDataError result = new();
            try
            {
                IsConnectedOneTime = true;
                connection = GetConnection();
                connection.Open();
                if (!keepConnectionOpen)
                {
                    connection.Close();
                }
                IsConnectedOneTime = true;
            }
            catch (Exception e)
            {
                if (e is MySqlException exception)
                {
                    if (exception.Number == 1049) // missing database
                    {
                        try
                        {
                            MySqlConnectionStringBuilder builder = new()
                            {
                                Server = host,
                                UserID = username,
                                Password = password
                            };
                            connection = new MySqlConnection(builder.ConnectionString);
                            connection.Open();
                            Execute("CREATE DATABASE " + database + ";");
                            connection.Close();


                            MySqlConnectionStringBuilder builderFull = new()
                            {
                                Server = host,
                                UserID = username,
                                Password = password,
                                Database = database
                            };
                            connection = new MySqlConnection(builder.ConnectionString);
                            connection.Open();
                            if (!keepConnectionOpen)
                            {
                                connection.Close();
                            }
                            IsConnectedOneTime = true;
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

        public override ResultWithDataError<DbCommand> CreateCmd(string sql)
        {
            ResultWithDataError<DbCommand> result = new();
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

        public override ResultWithDataError<bool> ResetStorage()
        {
            ResultWithDataError<bool> result = new();
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

        #region table
        protected override string PrepareSQLCreateTable(TableInfo table)
        {
            return Queries.CreateTable.GetQuery(table);
        }
        protected override string PrepareSQLCreateIntermediateTable(TableMemberInfo tableMember)
        {
            return Queries.CreateIntermediateTable.GetQuery(tableMember);
        }
        protected override string PrepareSQLTableExist(TableInfo table)
        {
            string sql = "SELECT COUNT(*) nb FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '" + table.SqlTableName + "' and TABLE_SCHEMA = '" + GetDatabaseName() + "'; ";
            return sql;
        }
        #endregion

        #region query
        protected override string PrepareSQLForQuery<X>(DatabaseQueryBuilder<X> queryBuilder)
        {
            return Queries.Query.PrepareSQL(queryBuilder, this);
        }

        #endregion

        #region exist
        protected override string PrepareSQLForExist<X>(DatabaseExistBuilder<X> queryBuilder)
        {
            return Queries.Exist.PrepareSQL(queryBuilder, this);
        }
        #endregion

        #region create
        protected override List<DatabaseCreateBuilderInfo> PrepareSQLForCreate<X>(DatabaseCreateBuilder<X> createBuilder)
        {
            return Queries.Create.PrepareSQL(createBuilder);
        }
        #endregion

        #region update
        protected override DatabaseUpdateBuilderInfo PrepareSQLForUpdate<X>(DatabaseUpdateBuilder<X> updateBuilder)
        {
            return Queries.Update.PrepareSQL(updateBuilder, this);
        }

        #endregion

        #region delete
        protected override string PrepareSQLForDelete<X>(DatabaseDeleteBuilder<X> deleteBuilder)
        {
            return Queries.Delete.PrepareSQL(deleteBuilder, this);
        }
       
        #endregion

        
        protected override object? TransformValueForFct(ParamsInfo paramsInfo)
        {
            if (paramsInfo.Value is string casted)
            {
                if (paramsInfo.FctMethodCall == WhereGroupFctEnum.StartsWith)
                {
                    return casted + "%";
                }
                if (paramsInfo.FctMethodCall == WhereGroupFctEnum.EndsWith)
                {
                    return "%" + casted;
                }
                if (paramsInfo.FctMethodCall == WhereGroupFctEnum.ContainsStr)
                {
                    return "%" + casted + "%";
                }
            }
            return paramsInfo.Value;
        }

        
        
    }
}
