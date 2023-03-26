using AventusSharp.Attributes;
using AventusSharp.Data.Storage.Default.Action;
using System;
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
        protected DbConnection connection;
        private Mutex mutex;
        private bool linksCreated;
        private DbTransaction transaction;
        public bool IsConnectedOneTime { get; protected set; }

        private Dictionary<Type, TableInfo> allTableInfos = new Dictionary<Type, TableInfo>();
        public TableInfo getTableInfo(Type type)
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
        public abstract DbCommand CreateCmd(string sql);
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
            DbCommand command = CreateCmd(sql);
            StorageExecutResult result = Execute(command, null, callerPath, callerNo);
            command.Dispose();
            return result;
        }
        public StorageExecutResult Execute(DbCommand command, List<Dictionary<string, object>> dataParameters, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
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
                DbTransaction transaction = this.transaction;
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
                        foreach (Dictionary<string, object> parameters in dataParameters)
                        {
                            foreach (KeyValuePair<string, object> parameter in parameters)
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
            DbCommand command = CreateCmd(sql);
            StorageQueryResult result = Query(command, null, callerPath, callerNo);
            command.Dispose();
            return result;
        }
        public StorageQueryResult Query(DbCommand command, List<Dictionary<string, object>> dataParameters, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
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
                DbTransaction transaction = this.transaction;
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
                        foreach (Dictionary<string, object> parameters in dataParameters)
                        {
                            foreach (KeyValuePair<string, object> parameter in parameters)
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
                                                temp.Add(reader.GetName(i), reader[reader.GetName(i)].ToString());
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
                                            temp.Add(reader.GetName(i), reader[reader.GetName(i)].ToString());
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
        public ResultWithError<bool> CommitTransaction(DbTransaction transaction)
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
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
        public ResultWithError<bool> RollbackTransaction(DbTransaction transaction)
        {
            ResultWithError<bool> result = new ResultWithError<bool>();
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
                Actions = defineActions();
                linksCreated = true;
                foreach (TableInfo info in allTableInfos.Values.ToList())
                {
                    foreach (TableMemberInfo memberInfo in info.members.ToList())
                    {
                        if (memberInfo.link != TableMemberInfoLink.None && memberInfo.TableLinked == null)
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
        private void AddPyramidLoop(PyramidInfo pyramid, TableInfo parent, List<TableMemberInfo> membersToAdd, bool typeMemberCreated)
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
                    TableMemberInfo createdDate = null;
                    TableMemberInfo updatedDate = null;
                    foreach (TableMemberInfo memberInfo in membersToAdd.ToList())
                    {
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

                    List<TableMemberInfo> prims = parent.primaries;
                    foreach (TableMemberInfo prim in prims)
                    {
                        classInfo.members.Insert(0, prim.TransformForParentLink(parent));
                    }
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

        #region Get

        #region GetAll
        public ResultWithError<List<X>> GetAll<X>() where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return GetAll<X>(allTableInfos[type]);
            }

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<List<X>> GetAll<X>(TableInfo pyramid) where X : IStorable
        {
            return Actions._GetAll.run<X>(pyramid);
        }
        #endregion

        #region GetById
        public ResultWithError<X> GetById<X>(int id) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return GetById<X>(allTableInfos[type], id);
            }

            ResultWithError<X> result = new ResultWithError<X>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<X> GetById<X>(TableInfo pyramid, int id) where X : IStorable
        {
            return Actions._GetById.run<X>(pyramid, id);
        }
        #endregion

        #region Where
        public ResultWithError<List<X>> Where<X>(Expression<Func<X, bool>> func) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return Where<X>(allTableInfos[type], func);
            }

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<List<X>> Where<X>(TableInfo pyramid, Expression<Func<X, bool>> func) where X : IStorable
        {
            return Actions._Where.run<X>(pyramid, func);
        }
        #endregion

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
            ResultWithError<DbTransaction> transactionResult = BeginTransaction();
            if (!transactionResult.Success)
            {
                ResultWithError<List<X>> resultError = new ResultWithError<List<X>>();
                resultError.Result = new List<X>();
                resultError.Errors = transactionResult.Errors;
                return resultError;
            }
            ResultWithError<List<X>> resultTemp = Actions._Create.run(pyramid, values);
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

        #endregion

        #region Update
        public ResultWithError<List<X>> Update<X>(List<X> values) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return Update(allTableInfos[type], values);
            }

            ResultWithError<List<X>> result = new ResultWithError<List<X>>();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + type + " inside the storage " + GetType().Name));
            return result;
        }

        public ResultWithError<List<X>> Update<X>(TableInfo pyramid, List<X> values) where X : IStorable
        {
            return Actions._Update.run(pyramid, values);
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
            return Actions._Delete.run(pyramid, values);
        }
        #endregion

        #endregion
    }
}
