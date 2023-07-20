using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Manager.DB.Create;
using AventusSharp.Data.Manager.DB.Delete;
using AventusSharp.Data.Manager.DB.Query;
using AventusSharp.Data.Manager.DB.Update;
using AventusSharp.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;

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

        public List<DataError> Errors = new();
    }

    public abstract class DefaultDBStorage<T> : IDBStorage where T : IDBStorage
    {
        protected string host;
        protected string username;
        protected string password;
        protected string database;
        protected bool keepConnectionOpen;
        protected bool addCreatedAndUpdatedDate;
        protected DbConnection? connection;
        private readonly Mutex mutex;
        private bool linksCreated;
        private DbTransaction? transaction;
        public bool IsConnectedOneTime { get; protected set; }

        private readonly Dictionary<Type, TableInfo> allTableInfos = new();
        public TableInfo? GetTableInfo(Type type)
        {
            if (allTableInfos.ContainsKey(type))
            {
                return allTableInfos[type];
            }
            return null;
        }
        public string GetDatabaseName() => database;

        public DefaultDBStorage(StorageCredentials info)
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
            return ConnectWithError().Success;
        }
        public virtual VoidWithError ConnectWithError()
        {
            VoidWithError result = new();
            try
            {
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
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
            }
            return result;
        }

        public abstract ResultWithError<bool> ResetStorage();
        protected abstract DbConnection GetConnection();
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
            if (commandResult.Result != null)
            {
                StorageExecutResult result = Execute(commandResult.Result, null, callerPath, callerNo);
                commandResult.Result.Dispose();
                return result;
            }
            StorageExecutResult noCommand = new();
            noCommand.Errors.AddRange(commandResult.Errors);
            return noCommand;
        }
        public StorageExecutResult Execute(DbCommand command, List<Dictionary<string, object?>>? dataParameters, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            if (connection == null)
            {
                StorageQueryResult noConn = new();
                noConn.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
                return noConn;
            }
            mutex.WaitOne();
            StorageExecutResult result = new();
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
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message, callerPath, callerNo));
                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = RollbackTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }

            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message, callerPath, callerNo));
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
            StorageQueryResult noCommand = new();
            noCommand.Errors.AddRange(commandResult.Errors);
            return noCommand;
        }
        public StorageQueryResult Query(DbCommand command, List<Dictionary<string, object?>>? dataParameters, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            if (connection == null)
            {
                StorageQueryResult noConn = new();
                noConn.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
                return noConn;
            }
            mutex.WaitOne();
            StorageQueryResult result = new();
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


                            using IDataReader reader = command.ExecuteReader();
                            while (reader.Read())
                            {
                                Dictionary<string, string> temp = new();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (!temp.ContainsKey(reader.GetName(i)))
                                    {
                                        if (reader[reader.GetName(i)] != null)
                                        {
                                            string? valueString = reader[reader.GetName(i)].ToString();
                                            valueString ??= "";
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
                    else
                    {
                        using IDataReader reader = command.ExecuteReader();
                        while (reader.Read())
                        {
                            Dictionary<string, string> temp = new();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                if (!temp.ContainsKey(reader.GetName(i)))
                                {
                                    if (reader[reader.GetName(i)] != null)
                                    {
                                        string? valueString = reader[reader.GetName(i)].ToString();
                                        valueString ??= "";
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

                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = CommitTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }
                catch (Exception e)
                {
                    DataError error = new DataError(DataErrorCode.UnknowError, e.Message, callerPath, callerNo);
                    error.Details.Add(command.CommandText);
                    result.Errors.Add(error);
                    if (isNewTransaction)
                    {
                        ResultWithError<bool> transactionResult = RollbackTransaction(transaction);
                        result.Errors.AddRange(transactionResult.Errors);
                    }
                }
            }
            catch (Exception e)
            {
                result.Errors.Add(new DataError(DataErrorCode.UnknowError, e.Message, callerPath, callerNo));
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
            ResultWithError<DbTransaction> result = new();
            if (connection == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.NoConnectionInsideStorage, "The storage " + GetType().Name, " doesn't have a connection"));
                return result;
            }
            try
            {
                transaction ??= connection.BeginTransaction();
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
            ResultWithError<bool> result = new();
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
            ResultWithError<bool> result = new();
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
        public VoidWithError CreateLinks()
        {
            VoidWithError result = new VoidWithError();
            if (!linksCreated)
            {
                linksCreated = true;
                foreach (TableInfo info in allTableInfos.Values.ToList())
                {
                    result = info.LoadDM();
                    if (!result.Success)
                    {
                        return result;
                    }
                    foreach (TableMemberInfo memberInfo in info.Members.ToList())
                    {
                        if (memberInfo.Link != TableMemberInfoLink.None && memberInfo.TableLinked == null && memberInfo.TableLinkedType != null)
                        {
                            if (allTableInfos.ContainsKey(memberInfo.TableLinkedType))
                            {
                                memberInfo.TableLinked = allTableInfos[memberInfo.TableLinkedType];
                            }
                        }
                    }
                }
            }
            return result;
        }
        public VoidWithError AddPyramid(PyramidInfo pyramid)
        {
            linksCreated = false;
            return AddPyramidLoop(pyramid, null, null, false);
        }
        private VoidWithError AddPyramidLoop(PyramidInfo pyramid, TableInfo? parent, List<TableMemberInfo>? membersToAdd, bool typeMemberCreated)
        {
            VoidWithError resultTemp;
            TableInfo classInfo = new(pyramid);
            if (pyramid.isForceInherit)
            {
                membersToAdd ??= new List<TableMemberInfo>();
                membersToAdd.AddRange(classInfo.Members);
                foreach (PyramidInfo child in pyramid.children)
                {
                    resultTemp = AddPyramidLoop(child, parent, membersToAdd, typeMemberCreated);
                    if(!resultTemp.Success)
                    {
                        return resultTemp;
                    }
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
                            memberInfo.IsUpdatable = false;
                            createdDate = memberInfo;
                        }
                        else if (memberInfo.Name == "updatedDate")
                        {
                            membersToAdd.Remove(memberInfo);
                            updatedDate = memberInfo;
                        }
                        if (memberInfo.IsPrimary)
                        {
                            classInfo.Primary = memberInfo;
                        }
                    }
                    classInfo.Members.InsertRange(0, membersToAdd);
                    if (addCreatedAndUpdatedDate)
                    {
                        if (createdDate != null)
                        {
                            classInfo.Members.Add(createdDate);
                        }
                        if (updatedDate != null)
                        {
                            classInfo.Members.Add(updatedDate);
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
                    TableMemberInfo? primInfo = parent.Primary?.TransformForParentLink(parent);
                    if (primInfo != null)
                    {
                        classInfo.Members.Insert(0, primInfo);
                        classInfo.Primary = primInfo;
                    }
                }
                foreach (PyramidInfo child in pyramid.children)
                {
                    resultTemp = AddPyramidLoop(child, classInfo, null, typeMemberCreated);
                    if (!resultTemp.Success)
                    {
                        return resultTemp;
                    }
                }
            }

            return new VoidWithError();
        }
        #endregion

        #region actions
        protected enum QueryParameterType
        {
            Normal,
            GrabValue
        }
        protected abstract object? TransformValueForFct(ParamsInfo paramsInfo);
        protected StorageQueryResult QueryGeneric(StorableAction action, string sql, Dictionary<ParamsInfo, QueryParameterType> parameters, IStorable? item = null)
        {
            List<string> errors = new();

            if (item != null)
            {
                errors.AddRange(item.IsValid(action));
            }
            if (errors.Count > 0)
            {
                StorageQueryResult queryResultTemp = new()
                {
                    Result = new List<Dictionary<string, string>>()
                };
                foreach (string error in errors)
                {
                    queryResultTemp.Errors.Add(new DataError(DataErrorCode.ValidationError, error));
                }
                return queryResultTemp;
            }

            string sqlToExecute = sql;
            Dictionary<ParamsInfo, QueryParameterType> parametersToUse = new();
            // check if parameters list
            foreach (KeyValuePair<ParamsInfo, QueryParameterType> parameterInfo in parameters)
            {
                if (parameterInfo.Key.Value is IList list)
                {
                    List<string> paramNames = new();
                    for (int i = 0; i < list.Count; i++)
                    {
                        paramNames.Add("@" + parameterInfo.Key.Name + "_" + i);
                        parametersToUse.Add(new ParamsInfo()
                        {
                            DbType = parameterInfo.Key.DbType,
                            MembersList = parameterInfo.Key.MembersList,
                            Name = parameterInfo.Key.Name + "_" + i,
                            TypeLvl0 = parameterInfo.Key.TypeLvl0,
                            Value = list[i],
                        }, parameterInfo.Value);
                    }
                    sqlToExecute = sqlToExecute.Replace("@" + parameterInfo.Key.Name, "(" + string.Join(",", paramNames) + ")");
                }
                else
                {
                    parametersToUse.Add(parameterInfo.Key, parameterInfo.Value);
                }
            }
            StorageQueryResult result = new();
            ResultWithError<DbCommand> cmdResult = CreateCmd(sqlToExecute);
            result.Errors.AddRange(cmdResult.Errors);
            if (!result.Success || cmdResult.Result == null)
            {
                return result;
            }
            DbCommand cmd = cmdResult.Result;
            Dictionary<string, object?> parametersValue = new();
            foreach (KeyValuePair<ParamsInfo, QueryParameterType> parameterInfo in parametersToUse)
            {
                DbParameter parameter = GetDbParameter();
                parameter.ParameterName = "@" + parameterInfo.Key.Name;
                parameter.DbType = parameterInfo.Key.DbType;
                cmd.Parameters.Add(parameter);
                if (parameterInfo.Value == QueryParameterType.GrabValue)
                {
                    if (Regex.IsMatch(parameterInfo.Key.Name, "(^|\\.)updatedDate$") || Regex.IsMatch(parameterInfo.Key.Name, "(^|\\.)createdDate$"))
                    {
                        parameterInfo.Key.Value = DateTime.Now;
                        if (item != null)
                        {
                            parameterInfo.Key.SetCurrentValueOnObject(item);
                        }
                    }
                    else if (item != null)
                    {
                        parameterInfo.Key.TypeLvl0 = item.GetType();
                        parameterInfo.Key.SetValue(item);
                    }
                    else
                    {
                        parameterInfo.Key.Value = null;
                    }

                    errors.AddRange(parameterInfo.Key.IsValueValid());
                }
                parametersValue["@" + parameterInfo.Key.Name] = TransformValueForFct(parameterInfo.Key);
            }
            StorageQueryResult queryResult;
            if (errors.Count > 0)
            {
                queryResult = new StorageQueryResult
                {
                    Result = new List<Dictionary<string, string>>()
                };
                foreach (string error in errors)
                {
                    queryResult.Errors.Add(new DataError(DataErrorCode.ValidationError, error));
                }
            }
            else
            {
                queryResult = Query(cmd, new List<Dictionary<string, object?>>() { parametersValue });
            }
            cmd.Dispose();
            return queryResult;

        }

        #region Get
        protected abstract string PrepareSQLForQuery<X>(DatabaseQueryBuilder<X> queryBuilder) where X : IStorable;
        public ResultWithError<List<X>> QueryFromBuilder<X>(DatabaseQueryBuilder<X> queryBuilder) where X : IStorable
        {
            ResultWithError<List<X>> result = new();

            string sql = "";
            if (queryBuilder.Sql != null)
            {
                sql = queryBuilder.Sql;
            }
            else
            {
                sql = PrepareSQLForQuery(queryBuilder);
                queryBuilder.Sql = sql;
            }

            StorageQueryResult queryResult = QueryGeneric(StorableAction.Read, sql, queryBuilder.WhereParamsInfo.ToDictionary(p => p.Value, p => QueryParameterType.Normal));

            result.Errors.AddRange(queryResult.Errors);
            if (queryResult.Success)
            {
                result.Result = new List<X>();
                DatabaseBuilderInfo baseInfo = queryBuilder.InfoByPath[""];

                for (int i = 0; i < queryResult.Result.Count; i++)
                {
                    Dictionary<string, string> itemFields = queryResult.Result[i];
                    ResultWithError<object> resultTemp = CreateObject(baseInfo, itemFields, queryBuilder.AllMembers);
                    if(resultTemp.Success && resultTemp.Result != null)
                    {
                        if (resultTemp.Result is X oCasted)
                        {
                            result.Result.Add(oCasted);
                        }
                        else
                        {
                            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Impossible to cast " + resultTemp.Result.GetType().Name + " into " + typeof(X).Name));
                        }
                    }
                    else
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                    }
                    
                }
            }

            return result;
        }
        protected ResultWithError<object> CreateObject(DatabaseBuilderInfo info, Dictionary<string, string> itemFields, bool allMembers)
        {
            ResultWithError<object> result = new ResultWithError<object>();
            string rootAlias = info.Alias;
            TableInfo rootTableInfo = info.TableInfo;

            while (rootTableInfo.Parent != null)
            {
                rootTableInfo = rootTableInfo.Parent;
            }
            if (rootTableInfo != info.TableInfo)
            {
                rootAlias = info.Parents[rootTableInfo];
            }

            object o;
            if (info.TableInfo.IsAbstract)
            {
                string fieldTypeName = rootAlias + "*" + TableInfo.TypeIdentifierName;
                if (!itemFields.ContainsKey(fieldTypeName))
                {
                    result.Errors.Add(new DataError(DataErrorCode.NoTypeIdentifierFoundInsideQuery, "Can't find the field " + TableInfo.TypeIdentifierName));
                    return result;
                }
                
                ResultWithError<Type> typeToCreate = TypeTools.GetTypeDataObject(itemFields[fieldTypeName]);
                if(!typeToCreate.Success || typeToCreate.Result == null)
                {
                    result.Errors.AddRange(typeToCreate.Errors);
                    return result;
                }
                o = TypeTools.CreateNewObj(typeToCreate.Result);
            }
            else
            {
                o = TypeTools.CreateNewObj(info.TableInfo.Type);
            }

            // TODO : optimize this method by storing needed values
            foreach (KeyValuePair<TableMemberInfo, DatabaseBuilderInfoMember> member in info.Members)
            {
                string alias = member.Value.Alias;
                TableMemberInfo memberInfo = member.Key;
                string key = alias + "*" + memberInfo.SqlName;
                if (itemFields.ContainsKey(key))
                {
                    if (memberInfo.Link == TableMemberInfoLink.None)
                    {
                        memberInfo.SetSqlValue(o, itemFields[key]);
                    }
                    else if (memberInfo.Link == TableMemberInfoLink.Simple)
                    {
                        if (itemFields[key] != "")
                        {
                            if (member.Value.UseDM)
                            {
                                object? oTemp = memberInfo.TableLinked?.DM?.GetById(int.Parse(itemFields[key]));
                                memberInfo.SetValue(o, oTemp);
                            }
                            else if (info.links.ContainsKey(memberInfo))
                            {
                                // loaded from the query
                                ResultWithError<object> oTemp = CreateObject(info.links[memberInfo], itemFields, allMembers);
                                if (oTemp.Success && oTemp.Result != null)
                                {
                                    memberInfo.SetValue(o, oTemp.Result);
                                }
                                else
                                {
                                    result.Errors.AddRange(oTemp.Errors);
                                }
                            }
                            else
                            {
                                result.Errors.Add(new DataError(DataErrorCode.UnknowError, "impossible?"));
                            }
                        }
                    }
                }
            }
            result.Result = o;
            return result;
        }
        public void LoadTableFieldQuery<X>(TableInfo tableInfo, string alias, DatabaseBuilderInfo baseInfo, List<string> path, List<Type> types, DatabaseQueryBuilder<X> queryBuilder) where X : IStorable
        {
            bool useShort = false;
            if (queryBuilder.UseShortObject)
            {
                if (path.Count == 0)
                {
                    useShort = false;
                }
                else if (queryBuilder.InfoByPath[""].TableInfo.DM is IDatabaseDM DM)
                {
                    useShort = DM.IsShortLink(string.Join(".", path));
                }
            }

            if (useShort)
            {
                if (tableInfo.TypeMember != null)
                {
                    baseInfo.Members.Add(tableInfo.TypeMember, new DatabaseBuilderInfoMember(tableInfo.TypeMember, alias, this));
                }
                if (tableInfo.Primary != null)
                {
                    baseInfo.Members.Add(tableInfo.Primary, new DatabaseBuilderInfoMember(tableInfo.Primary, alias, this));
                }
            }
            else
            {
                foreach (TableMemberInfo member in tableInfo.Members)
                {
                    if (member.Link == TableMemberInfoLink.None)
                    {
                        baseInfo.Members.Add(member, new DatabaseBuilderInfoMember(member, alias, this));
                    }
                    else if (member.Link == TableMemberInfoLink.Simple)
                    {
                        DatabaseBuilderInfoMember info = new(member, alias, this);
                        if (!info.UseDM)
                        {
                            if (member.Type != null)
                            {
                                path.Add(member.Name);
                                types.Add(member.Type);
                                queryBuilder.LoadLinks(path, types, true);
                                path.RemoveAt(path.Count - 1);
                                types.RemoveAt(types.Count - 1);
                            }
                        }
                        else
                        {
                            baseInfo.Members.Add(member, info);
                        }
                    }
                }
            }

        }
        #endregion

        #region Table
        protected abstract string PrepareSQLCreateTable(TableInfo table);
        protected abstract string PrepareSQLCreateIntermediateTable(TableMemberInfo tableMember);
        public VoidWithError CreateTable(PyramidInfo pyramid)
        {
            VoidWithError result = new();
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
            VoidWithError result = new();
            ResultWithError<bool> tableExist = TableExist(table);
            result.Errors.AddRange(tableExist.Errors);
            if (tableExist.Success && !tableExist.Result)
            {
                string sql = PrepareSQLCreateTable(table);
                StorageExecutResult resultTemp = Execute(sql);
                result.Errors.AddRange(resultTemp.Errors);

                // create intermediate table
                List<TableMemberInfo> members = table.Members.Where
                    (f => f.Link == TableMemberInfoLink.Multiple).ToList();

                string? intermediateQuery = null;
                foreach (TableMemberInfo member in members)
                {
                    intermediateQuery = PrepareSQLCreateIntermediateTable(member);
                    StorageExecutResult resultTempInter = Execute(sql);
                    result.Errors.AddRange(resultTempInter.Errors);
                }
            }
            foreach (TableInfo child in table.Children)
            {
                VoidWithError resultTemp = CreateTable(child);
                result.Errors.AddRange(resultTemp.Errors);
            }
            return result;
        }

        public ResultWithError<bool> TableExist(PyramidInfo pyramid)
        {
            if (allTableInfos.ContainsKey(pyramid.type))
            {
                return TableExist(allTableInfos[pyramid.type]);
            }
            ResultWithError<bool> result = new();
            result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + pyramid.type));
            result.Result = false;
            return result;
        }
        protected abstract string PrepareSQLTableExist(TableInfo table);
        public ResultWithError<bool> TableExist(TableInfo table)
        {
            ResultWithError<bool> result = new();
            string sql = PrepareSQLTableExist(table);
            StorageQueryResult queryResult = Query(sql);
            result.Errors.AddRange(queryResult.Errors);

            if (queryResult.Success && queryResult.Result.Count == 1)
            {
                int nb = int.Parse(queryResult.Result.ElementAt(0)["nb"]);
                result.Result = (nb != 0);
            }
            return result;
        }
        #endregion

        #region Create

        protected abstract List<DatabaseCreateBuilderInfo> PrepareSQLForCreate<X>(DatabaseCreateBuilder<X> createBuilder) where X : IStorable;
        public ResultWithError<int> CreateFromBuilder<X>(DatabaseCreateBuilder<X> createBuilder, X item) where X : IStorable
        {
            ResultWithError<int> result = new()
            {
                Result = 0
            };
            if (item == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "Please provide an item to use for creation"));
                return result;
            }
            List<DatabaseCreateBuilderInfo> queries;
            if (createBuilder.queries != null)
            {
                queries = createBuilder.queries;
            }
            else
            {
                queries = PrepareSQLForCreate(createBuilder);
                createBuilder.queries = queries;
            }

            #region create
            int id = 0;
            foreach (DatabaseCreateBuilderInfo query in queries)
            {
                string sql = query.Sql;
                Dictionary<ParamsInfo, QueryParameterType> parametersCreate = new();
                foreach (ParamsInfo parameterInfo in query.Parameters)
                {
                    parametersCreate.Add(parameterInfo, QueryParameterType.GrabValue);
                }
                if (!query.HasPrimaryResult && query.PrimaryToSet != null)
                {
                    query.PrimaryToSet.Value = id;
                    parametersCreate.Add(query.PrimaryToSet, QueryParameterType.Normal);
                }

                StorageQueryResult createResult = QueryGeneric(StorableAction.Create, sql, parametersCreate, item);
                
                if (!createResult.Success)
                {
                    result.Errors.AddRange(createResult.Errors);
                    return result;
                }
                else if (query.HasPrimaryResult && createResult.Result != null)
                {
                    id = int.Parse(createResult.Result[0]["id"]);
                }
            }

            if (result.Errors.Count == 0)
            {
                result.Result = id;
            }
            #endregion

            return result;
        }

        #endregion

        #region Update
        protected abstract string PrepareSQLForUpdate<X>(DatabaseUpdateBuilder<X> updateBuilder) where X : IStorable;
        public ResultWithError<List<int>> UpdateFromBuilder<X>(DatabaseUpdateBuilder<X> updateBuilder, X item) where X : IStorable
        {
            ResultWithError<List<int>> result = new()
            {
                Result = new List<int>()
            };
            if (item == null)
            {
                result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "Please provide an item to use for update"));
                return result;
            }
            string sql;
            if (updateBuilder.Sql != null)
            {
                sql = updateBuilder.Sql;
            }
            else
            {
                sql = PrepareSQLForUpdate(updateBuilder);
                updateBuilder.Sql = sql;
            }


            Dictionary<ParamsInfo, QueryParameterType> parametersUpdate = new();
            Dictionary<ParamsInfo, QueryParameterType> parametersQuery = new();
            foreach (KeyValuePair<string, ParamsInfo> parameterInfo in updateBuilder.WhereParamsInfo)
            {
                parametersUpdate.Add(parameterInfo.Value, QueryParameterType.Normal);
                parametersQuery.Add(parameterInfo.Value, QueryParameterType.Normal);
            }

            foreach (KeyValuePair<string, ParamsInfo> parameterInfo in updateBuilder.UpdateParamsInfo)
            {
                parametersUpdate.Add(parameterInfo.Value, QueryParameterType.GrabValue);
            }
            #region query elements that will be updated
            StorageQueryResult queryResult = QueryGeneric(StorableAction.Read, updateBuilder.SqlQuery, parametersQuery);
            List<int> list = new();
            if (!queryResult.Success)
            {
                result.Errors.AddRange(queryResult.Errors);
                return result;
            }
            else if (queryResult.Result != null)
            {
                foreach (Dictionary<string, string> row in queryResult.Result)
                {
                    if (row.ContainsKey("id"))
                    {
                        list.Add(int.Parse(row["id"]));
                    }
                }
            }
            #endregion

            #region update
            StorageQueryResult updateResult = QueryGeneric(StorableAction.Update, sql, parametersUpdate, item);
            if (!updateResult.Success)
            {
                result.Errors.AddRange(updateResult.Errors);
                return result;
            }
            #endregion
            result.Result = list;

            return result;
        }


        #endregion

        #region Delete
        protected abstract string PrepareSQLForDelete<X>(DatabaseDeleteBuilder<X> deleteBuilder) where X : IStorable;
        public VoidWithError DeleteFromBuilder<X>(DatabaseDeleteBuilder<X> deleteBuilder) where X : IStorable
        {
            VoidWithError result = new();
            string sql;
            if (deleteBuilder.Sql != null)
            {
                sql = deleteBuilder.Sql;
            }
            else
            {
                sql = PrepareSQLForDelete(deleteBuilder);
                deleteBuilder.Sql = sql;
            }


            Dictionary<ParamsInfo, QueryParameterType> parametersDelete = new();
            foreach (KeyValuePair<string, ParamsInfo> parameterInfo in deleteBuilder.WhereParamsInfo)
            {
                parametersDelete.Add(parameterInfo.Value, QueryParameterType.Normal);
            }

            #region delete
            StorageQueryResult deleteResult = QueryGeneric(StorableAction.Delete, sql, parametersDelete);
            if (!deleteResult.Success)
            {
                result.Errors.AddRange(deleteResult.Errors);
                return result;
            }
            #endregion

            return result;
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
        public ResultWithError<Dictionary<TableInfo, IList>> GroupDataByType<X>(IList data)
        {
            Type typeX = typeof(X);
            if (allTableInfos.ContainsKey(typeX))
            {
                TableInfo table = allTableInfos[typeX];
                ResultWithError<Dictionary<TableInfo, IList>> result = new()
                {
                    Result = new Dictionary<TableInfo, IList>()
                };
                if (table.IsAbstract)
                {
                    Dictionary<Type, TableInfo> loadedType = new();
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
            else
            {
                ResultWithError<Dictionary<TableInfo, IList>> result = new();
                result.Errors.Add(new DataError(DataErrorCode.TypeNotExistInsideStorage, "Can't find the type " + typeX + " inside the storage " + GetType().Name));
                return result;
            }


        }

        /// <summary>
        /// Run a function inside a transaction that ll be commit if no error otherwise rollback
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <param name="defaultValue"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public ResultWithError<Y> RunInsideTransaction<Y>(Y defaultValue, Func<ResultWithError<Y>> action)
        {
            ResultWithError<DbTransaction> transactionResult = BeginTransaction();
            if (!transactionResult.Success)
            {
                ResultWithError<Y> resultError = new()
                {
                    Result = defaultValue,
                    Errors = transactionResult.Errors
                };
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
            List<TableMemberInfo> result = new();
            TableInfo? tableInfo = GetTableInfo(type);
            if (tableInfo != null)
            {

            }
            return result;
        }

        #endregion
    }
}
