using AventusSharp.Attributes;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default
{
    public class StorageCredentials
    {
        public string host;
        public string username;
        public string password;
        public string database;
        public bool keepConnectionOpen;
        public bool addCreatedAndUpdatedDate = true;

        public StorageCredentials(string host, string username, string password, string database)
        {
            this.host = host;
            this.username = username;
            this.password = password;
            this.database = database;
        }
    }

    public class StorageQueryResult : StorageExecutResult
    {
        public List<Dictionary<string, string>> Result { get; set; } = new List<Dictionary<string, string>>();
    }
    public class StorageExecutResult
    {
        public bool Success { get => Errors.Count == 0; }

        public List<DataError> Errors = new List<DataError>();
    }

    public abstract class DefaultStorage<T> : IStorage where T : IStorage
    {
        protected string host;
        protected string username;
        protected string password;
        protected string database;
        protected bool keepConnectionOpen;
        protected bool addCreatedAndUpdatedDate;
        protected DbConnection? connection;
        private Mutex mutex;
        private bool linksCreated;
        private DbTransaction? transaction;
        public bool IsConnectedOneTime { get; protected set; }

        private Dictionary<Type, TableInfo> allTableInfos = new Dictionary<Type, TableInfo>();
        public TableInfo? GetTableInfo(Type type)
        {
            if (allTableInfos.ContainsKey(type))
            {
                return allTableInfos[type];
            }
            return null;
        }
        public string GetDatabaseName() => database;

        public DefaultStorage(StorageCredentials info)
        {
            host = info.host;
            username = info.username;
            password = info.password;
            database = info.database;
            keepConnectionOpen = info.keepConnectionOpen;
            addCreatedAndUpdatedDate = info.addCreatedAndUpdatedDate;
            mutex = new Mutex();
            Actions = defineActions();
        }

        #region connection
        public bool Connect()
        {
            return ConnetWithError().Success;
        }
        public virtual ResultWithError<bool> ConnetWithError()
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
            try
            {
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
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
            return result;
        }

        public abstract ResultWithError<bool> ResetStorage();
        protected abstract DbConnection getConnection();
        public abstract ResultWithError<DbCommand> CreateCmd(string sql);
        public abstract DbParameter GetDbParameter();
        public void Close()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                    transaction.Dispose();
                }
            }
            catch (Exception e)
            {
                new DataError(DataErrorCode.UnknowError, e.Message).Print();
            }
            try
            {
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
            catch (Exception e)
            {
                new DataError(DataErrorCode.UnknowError, e.Message).Print();
            }
        }

        public StorageExecutResult Execute(string sql, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            ResultWithError<DbCommand> commandResult = CreateCmd(sql);
            if(commandResult.Result != null)
            {
                StorageExecutResult result = Execute(commandResult.Result, null, callerPath, callerNo);
                commandResult.Result.Dispose();
                return result;
            }
            StorageExecutResult noCommand = new StorageExecutResult();
            noCommand.Errors.AddRange(commandResult.Errors);
            return noCommand;
        }
        public StorageExecutResult Execute(DbCommand command, List<Dictionary<string, object?>>? dataParameters, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            if (connection == null)
            {
                StorageQueryResult noConn = new StorageQueryResult();
                noConn.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
                return noConn;
            }
            mutex.WaitOne();
            StorageExecutResult result = new StorageExecutResult();
            if (!keepConnectionOpen || connection.State == ConnectionState.Closed)
            {
                if (!Connect())
                {
                    mutex.ReleaseMutex();
                    result.Errors.Add(new DataError(DataErrorCode.StorageDisconnected, "The storage " + GetType().Name, " can't connect to the database"));
                    return result;
                }
            }

            try
            {
                bool isNewTransaction = false;
                DbTransaction? transaction = this.transaction;
                if (transaction == null)
                {
                    ResultWithError<DbTransaction> resultTransaction = BeginTransaction();
                    result.Errors.AddRange(resultTransaction.Errors);
                    if (!result.Success)
                    {
                        return result;
                    }
                    transaction = resultTransaction.Result;
                    isNewTransaction = true;
                }

                command.Transaction = transaction;
                command.Connection = connection;
                try
                {
                    if (dataParameters != null)
                    {
                        foreach (Dictionary<string, object?> parameters in dataParameters)
                        {
                            foreach (KeyValuePair<string, object?> parameter in parameters)
                            {
                                command.Parameters[parameter.Key].Value = parameter.Value;
                            }
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        command.ExecuteNonQuery();
                    }
                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = CommitTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }
                catch (Exception e)
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message));
                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = RollbackTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }

            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message));
            }
            if (!keepConnectionOpen)
            {
                Close();
            }
            mutex.ReleaseMutex();
            return result;
        }
        public StorageQueryResult Query(string sql, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            ResultWithError<DbCommand> commandResult = CreateCmd(sql);
            if (commandResult.Result != null)
            {
                StorageQueryResult result = Query(commandResult.Result, null, callerPath, callerNo);
                commandResult.Result.Dispose();
                return result;
            }
            StorageQueryResult noCommand = new StorageQueryResult();
            noCommand.Errors.AddRange(commandResult.Errors);
            return noCommand;
        }
        public StorageQueryResult Query(DbCommand command, List<Dictionary<string, object?>>? dataParameters, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            if(connection == null)
            {
                StorageQueryResult noConn = new StorageQueryResult();
                noConn.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
                return noConn;
            }
            mutex.WaitOne();
            StorageQueryResult result = new StorageQueryResult();
            if (!keepConnectionOpen || connection.State == ConnectionState.Closed)
            {
                if (!Connect())
                {
                    mutex.ReleaseMutex();
                    result.Errors.Add(new DataError(DataErrorCode.StorageDisconnected, "The storage " + GetType().Name, " can't connect to the database"));
                    return result;
                }
            }

            try
            {

                bool isNewTransaction = false;
                DbTransaction? transaction = this.transaction;
                if (transaction == null)
                {
                    ResultWithError<DbTransaction> resultTransaction = BeginTransaction();
                    result.Errors.AddRange(resultTransaction.Errors);
                    if (!result.Success)
                    {
                        return result;
                    }
                    transaction = resultTransaction.Result;
                    isNewTransaction = true;
                }

                command.Transaction = transaction;
                command.Connection = connection;
                try
                {
                    if (dataParameters != null)
                    {
                        foreach (Dictionary<string, object?> parameters in dataParameters)
                        {
                            foreach (KeyValuePair<string, object?> parameter in parameters)
                            {
                                command.Parameters[parameter.Key].Value = parameter.Value;
                            }


                            using (IDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Dictionary<string, string> temp = new Dictionary<string, string>();
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        if (!temp.ContainsKey(reader.GetName(i)))
                                        {
                                            if (reader[reader.GetName(i)] != null)
                                            {
                                                string? valueString = reader[reader.GetName(i)].ToString();
                                                if(valueString == null)
                                                {
                                                    valueString = "";
                                                }
                                                temp.Add(reader.GetName(i), valueString);
                                            }
                                            else
                                            {
                                                temp.Add(reader.GetName(i), "");
                                            }
                                        }
                                    }
                                    result.Result.Add(temp);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (IDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Dictionary<string, string> temp = new Dictionary<string, string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (!temp.ContainsKey(reader.GetName(i)))
                                    {
                                        if (reader[reader.GetName(i)] != null)
                                        {
                                            string? valueString = reader[reader.GetName(i)].ToString();
                                            if(valueString == null)
                                            {
                                                valueString = "";
                                            }
                                            temp.Add(reader.GetName(i), valueString);
                                        }
                                        else
                                        {
                                            temp.Add(reader.GetName(i), "");
                                        }
                                    }
                                }
                                result.Result.Add(temp);
                            }
                        }
                    }

                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = CommitTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }
                catch (Exception e)
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message));
                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = RollbackTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message));
            }
            if (!keepConnectionOpen)
            {
                Close();
            }
            mutex.ReleaseMutex();
            return result;
        }

        public ResultWithError<DbTransaction> BeginTransaction()
        {
            ResultWithError<DbTransaction> result = new ResultWithError<DbTransaction>();
            if (connection == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
                return result;
            }
            try
            {
                if (transaction == null)
                {
                    transaction = connection.BeginTransaction();
                }
                result.Result = transaction;
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
            return result;
        }
        public ResultWithError<bool> CommitTransaction()
        {
            return CommitTransaction(transaction);
        }
        public ResultWithError<bool> CommitTransaction(DbTransaction? transaction)
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
            if (transaction == null)
            {
                result.Result = true;
                return result;
            }
            lock (transaction)
            {
                if (transaction == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.NoTransactionInProgress, "There is no transation to commit"));
                }
                else
                {
                    try
                    {
                        transaction.Commit();
                        result.Result = true;
                        transaction.Dispose();
                        if (transaction == this.transaction)
                        {
                            this.transaction = null;
                        }
                    }
                    catch (Exception e)
                    {
                        result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                    }
                }
            }
            return result;
        }
        public ResultWithError<bool> RollbackTransaction()
        {
            return RollbackTransaction(transaction);
        }
        public ResultWithError<bool> RollbackTransaction(DbTransaction? transaction)
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
            if (transaction == null)
            {
                result.Result = true;
                return result;
            }
            lock (transaction)
            {
                if (transaction == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.NoTransactionInProgress, "There is no transation to rollback"));
                }
                else
                {
                    try
                    {
                        transaction.Rollback();
                        result.Result = true;
                        transaction.Dispose();
                        if (transaction == this.transaction)
                        {
                            this.transaction = null;
                        }
                    }
                    catch (Exception e)
                    {
                        result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                    }
                }
            }
            return result;
        }
        #endregion

        #region init
        public void CreateLinks()
        {
            if (!linksCreated)
            {
                linksCreated = true;
                foreach (TableInfo info in allTableInfos.Values.ToList())
                {
                    foreach (TableMemberInfo memberInfo in info.members.ToList())
                    {
                        if (memberInfo.link != TableMemberInfoLink.None && memberInfo.TableLinked == null && memberInfo.TableLinkedType != null)
                        {
                            if (allTableInfos.ContainsKey(memberInfo.TableLinkedType))
                            {
                                memberInfo.TableLinked = allTableInfos[memberInfo.TableLinkedType];
                            }
                        }
                    }
                }
            }
        }
        public void AddPyramid(PyramidInfo pyramid)
        {
            AddPyramidLoop(pyramid, null, null, false);
        }
        private void AddPyramidLoop(PyramidInfo pyramid, TableInfo? parent, List<TableMemberInfo>? membersToAdd, bool typeMemberCreated)
        {
            TableInfo classInfo = new TableInfo(pyramid);
            if (pyramid.isForceInherit)
            {
                if (membersToAdd == null)
                {
                    membersToAdd = new List<TableMemberInfo>();
                }
                membersToAdd.AddRange(classInfo.members);
                foreach (PyramidInfo child in pyramid.children)
                {
                    AddPyramidLoop(child, parent, membersToAdd, typeMemberCreated);
                }
            }
            else
            {

                if (membersToAdd != null)
                {
                    // merge parent members
                    // force created and updated date to the end
                    TableMemberInfo? createdDate = null;
                    TableMemberInfo? updatedDate = null;
                    foreach (TableMemberInfo memberInfo in membersToAdd.ToList())
                    {
                        memberInfo.ChangeTableInfo(classInfo);
                        if (memberInfo.Name == "createdDate")
                        {
                            membersToAdd.Remove(memberInfo);
                            createdDate = memberInfo;
                        }
                        else if (memberInfo.Name == "updatedDate")
                        {
                            membersToAdd.Remove(memberInfo);
                            updatedDate = memberInfo;
                        }
                        if (memberInfo.IsPrimary)
                        {
                            classInfo.primary = memberInfo;
                        }
                    }
                    classInfo.members.InsertRange(0, membersToAdd);
                    if (addCreatedAndUpdatedDate)
                    {
                        if (createdDate != null)
                        {
                            classInfo.members.Add(createdDate);
                        }
                        if (updatedDate != null)
                        {
                            classInfo.members.Add(updatedDate);
                        }
                    }
                }
                if (classInfo.IsAbstract && !typeMemberCreated)
                {
                    classInfo.AddTypeMember();
                    typeMemberCreated = true;
                }
                allTableInfos[pyramid.type] = classInfo;
                if (pyramid.aliasType != null)
                {
                    allTableInfos[pyramid.aliasType] = classInfo;
                }
                if (parent != null)
                {
                    classInfo.Parent = parent;
                    parent.Children.Add(classInfo);
                    TableMemberInfo primInfo = parent.primary.TransformForParentLink(parent);
                    classInfo.members.Insert(0, primInfo);
                    classInfo.primary = primInfo;
                }
                foreach (PyramidInfo child in pyramid.children)
                {
                    AddPyramidLoop(child, classInfo, null, typeMemberCreated);
                }
            }


        }
        #endregion

        #region actions
        private StorageAction<T> Actions;
        internal abstract StorageAction<T> defineActions();
        public abstract ResultWithError<List<X>> QueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder);
        public abstract ResultWithError<X> UpdateFromBuilder<X>(DatabaseUpdateBuilder<X> queryBuilder, X item);


        #region Table

        public VoidWithError CreateTable(PyramidInfo pyramid)
        {
            VoidWithError result = new VoidWithError();
            if (!pyramid.isForceInherit)
            {
                if (allTableInfos.ContainsKey(pyramid.type))
                {
                    VoidWithError resultTemp = CreateTable(allTableInfos[pyramid.type]);
                    result.Errors.AddRange(resultTemp.Errors);
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + pyramid.type));
                }
            }
            else
            {
                foreach (PyramidInfo child in pyramid.children)
                {
                    VoidWithError resultTemp = CreateTable(child);
                    result.Errors.AddRange(resultTemp.Errors);
                }
            }
            return result;
        }
        public VoidWithError CreateTable(TableInfo table)
        {
            return Actions._CreateTable.run(table);
        }

        public ResultWithError<bool> TableExist(PyramidInfo pyramid)
        {
            if (allTableInfos.ContainsKey(pyramid.type))
            {
                return TableExist(allTableInfos[pyramid.type]);
            }
            ResultWithError<bool> result = new ResultWithError<bool>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + pyramid.type));
            result.Result = false;
            return result;
        }
        public ResultWithError<bool> TableExist(TableInfo table)
        {
            return Actions._TableExist.run(table);
        }
        #endregion


        #region Create
        public ResultWithError<List<X>> Create<X>(List<X> values) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return Create(allTableInfos[type], values);
            }

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<List<X>> Create<X>(TableInfo pyramid, List<X> values) where X : IStorable
        {
            return RunInsideTransaction(new List<X>(), delegate ()
            {
                return Actions._Create.run(pyramid, values);
            });
        }

        #endregion

        #region Update
        public ResultWithError<List<X>> Update<X>(List<X> values, List<X>? oldValues = null) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return Update(allTableInfos[type], values, oldValues);
            }

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<List<X>> Update<X>(TableInfo pyramid, List<X> values, List<X>? oldValues) where X : IStorable
        {
            return RunInsideTransaction(new List<X>(), delegate ()
            {
                return Actions._Update.run(pyramid, values, oldValues);
            });
        }
        #endregion

        #region Delete
        public ResultWithError<List<X>> Delete<X>(List<X> values) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return Delete(allTableInfos[type], values);
            }

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<List<X>> Delete<X>(TableInfo pyramid, List<X> values) where X : IStorable
        {
            return RunInsideTransaction(new List<X>(), delegate ()
            {
                return Actions._Delete.run(pyramid, values);
            });
        }
        #endregion

        #endregion

        #region Tools
        /// <summary>
        /// Order data but type
        /// </summary>
        /// <param name="table"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ResultWithError<Dictionary<TableInfo, IList>> GroupDataByType(TableInfo table, IList data)
        {
            ResultWithError<Dictionary<TableInfo, IList>> result = new ResultWithError<Dictionary<TableInfo, IList>>();
            result.Result = new Dictionary<TableInfo, IList>();
            if (table.IsAbstract)
            {
                Dictionary<Type, TableInfo> loadedType = new Dictionary<Type, TableInfo>();
                foreach (object item in data)
                {
                    Type type = item.GetType();
                    if (!loadedType.ContainsKey(type))
                    {
                        TableInfo? tableInfo = GetTableInfo(type);
                        if (tableInfo == null)
                        {
                            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "this must be impossible"));
                            return result;
                        }
                        else
                        {
                            loadedType.Add(type, tableInfo);
                            Type newListType = typeof(List<>).MakeGenericType(type);
                            IList newList = TypeTools.CreateNewObj<IList>(newListType);
                            result.Result.Add(tableInfo, newList);
                        }
                    }
                    result.Result[loadedType[type]].Add(item);
                }
            }
            else
            {
                result.Result.Add(table, data);
            }
            return result;
        }

        /// <summary>
        /// Run a function inside a transaction that ll be commit if no error otherwise rollback
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <param name="defaultValue"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        protected ResultWithError<Y> RunInsideTransaction<Y>(Y defaultValue, Func<ResultWithError<Y>> action)
        {
            ResultWithError<DbTransaction> transactionResult = BeginTransaction();
            if (!transactionResult.Success)
            {
                ResultWithError<Y> resultError = new ResultWithError<Y>();
                resultError.Result = defaultValue;
                resultError.Errors = transactionResult.Errors;
                return resultError;
            }
            ResultWithError<Y> resultTemp = action();
            if (resultTemp.Success)
            {
                ResultWithError<bool> commitResult = CommitTransaction(transactionResult.Result);
                resultTemp.Errors.AddRange(commitResult.Errors);
            }
            else
            {
                ResultWithError<bool> commitResult = RollbackTransaction(transactionResult.Result);
                resultTemp.Errors.AddRange(commitResult.Errors);
            }
            if (!resultTemp.Success)
            {
                foreach (DataError error in resultTemp.Errors)
                {
                    error.Print();
                }
            }
            return resultTemp;
        }

        public List<TableMemberInfo> GetTableMemberInfosForType(Type type)
        {
            List<TableMemberInfo> result = new List<TableMemberInfo>();
            TableInfo? tableInfo = GetTableInfo(type);
            if (tableInfo != null)
            {

            }
            return result;
        }

        #endregion
    }
}
