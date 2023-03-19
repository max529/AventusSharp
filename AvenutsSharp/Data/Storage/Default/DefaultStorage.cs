using AventusSharp.Attributes;
using AventusSharp.Data.Storage.Default.Action;
using AventusSharp.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
        public virtual bool Connect()
        {
            try
            {
                connection = getConnection();
                connection.Open();
                if (!keepConnectionOpen)
                {
                    connection.Close();
                }
                this.IsConnectedOneTime = true;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        protected abstract DbConnection getConnection();
        protected abstract DbCommand CreateCmd(string sql);
        private void Close()
        {
            connection.Close();
        }

        public void Execute(string sql, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {

            mutex.WaitOne();
            if (!keepConnectionOpen || connection.State == ConnectionState.Closed)
            {
                if (!Connect())
                {
                    mutex.ReleaseMutex();
                    return;
                }
            }

            try
            {
                if (sql != "")
                {
                    using (DbCommand command = CreateCmd(sql))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                if (!keepConnectionOpen)
                {
                    Close();
                }
                mutex.ReleaseMutex();
            }
            catch (Exception e)
            {
                if (!keepConnectionOpen)
                {
                    Close();
                }
                mutex.ReleaseMutex();
                Console.WriteLine(e);
            }

        }

        public List<Dictionary<string, string>> Query(string sql, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            mutex.WaitOne();
            if (!keepConnectionOpen || connection.State == ConnectionState.Closed)
            {
                if (!Connect())
                {
                    mutex.ReleaseMutex();
                    return new List<Dictionary<string, string>>();
                }
            }

            try
            {
                using (DbCommand command = CreateCmd(sql))
                {
                    using (IDataReader reader = command.ExecuteReader())
                    {
                        List<Dictionary<string, string>> res = new List<Dictionary<string, string>>();
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
                            res.Add(temp);
                        }
                        Close();
                        mutex.ReleaseMutex();
                        return res;
                    }
                }
            }
            catch (Exception e)
            {
                if (!keepConnectionOpen)
                {
                    Close();
                }
                mutex.ReleaseMutex();
                Console.WriteLine(e);
            }
            return new List<Dictionary<string, string>>();
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
            AddPyramidLoop(pyramid);
        }
        private void AddPyramidLoop(PyramidInfo pyramid, TableInfo parent = null, List<TableMemberInfo> membersToAdd = null)
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
                    AddPyramidLoop(child, parent, membersToAdd);
                }
            }
            else
            {
                if (membersToAdd != null)
                {
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
                    foreach(TableMemberInfo prim in prims)
                    {
                        classInfo.members.Insert(0, prim.TransformForParentLink(parent));
                    }
                }
                foreach (PyramidInfo child in pyramid.children)
                {
                    AddPyramidLoop(child, classInfo);
                }
            }


        }
        #endregion

        #region actions
        private StorageAction<T> Actions;
        internal abstract StorageAction<T> defineActions();

        #region Table
        public void CreateTable(PyramidInfo pyramid)
        {
            if (!pyramid.isForceInherit)
            {
                if (allTableInfos.ContainsKey(pyramid.type))
                {
                    CreateTable(allTableInfos[pyramid.type]);
                }
                else
                {
                    LogError.getInstance().WriteLine("Can't find the type " + pyramid.type);
                }
            }
            else
            {
                foreach(PyramidInfo child in pyramid.children)
                {
                    CreateTable(child);
                }
            }
        }
        public void CreateTable(TableInfo table)
        {
            Actions._CreateTable.run(table);
        }

        public bool TableExist(PyramidInfo pyramid)
        {
            if (allTableInfos.ContainsKey(pyramid.type))
            {
                return TableExist(allTableInfos[pyramid.type]);
            }
            LogError.getInstance().WriteLine("Can't find the type " + pyramid.type);
            return false;
        }
        public bool TableExist(TableInfo table)
        {
            return Actions._TableExist.run(table);
        }
        #endregion

        #region Get

        #endregion

        #region Insert
        public List<X> Create<X>(List<X> values) where X : IStorable
        {
            Type type = typeof(X);
            if (allTableInfos.ContainsKey(type))
            {
                return Insert(allTableInfos[type], values);
            }
            LogError.getInstance().WriteLine("Can't find the type " + type);
            return new List<X>();
        }

        public List<X> Insert<X>(TableInfo pyramid, List<X> values) where X : IStorable
        {
            return Actions._Insert.run(pyramid, values);
        }

        #endregion

        #region Update

        #endregion

        #region Delete

        #endregion

        #endregion
    }
}
