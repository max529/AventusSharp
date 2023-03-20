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

        public override bool Connect()
        {
            try
            {
                IsConnectedOneTime = true;
                connection = getConnection();
                connection.Open();
                if (!keepConnectionOpen)
                {
                    connection.Close();
                }
                return true;
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
                            return true;
                        }
                        catch (Exception e2)
                        {
                            Console.WriteLine(e2);
                        }
                    }

                }
                return false;
            }
        }

        public override DbCommand CreateCmd(string sql)
        {
            MySqlCommand command = ((MySqlConnection)connection).CreateCommand();
            command.CommandType = System.Data.CommandType.Text;
            command.CommandText = sql;
            return command;
        }

        internal override StorageAction<MySQLStorage> defineActions()
        {
            return new MySQLAction(this);
        }
    }
}
