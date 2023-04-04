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
            foreach(Dictionary<string, string> line in queryResult.Result)
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
    }
}
