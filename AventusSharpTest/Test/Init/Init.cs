using AventusSharp.Data;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql;
using AventusSharpTest.Attribute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Test.Init
{
    [TestFixture]
    [Order(1)]
    [StopOnFail]
    public class Init
    {
        private MySQLStorage defaultStorage;

        [Test]
        public void TestConnection()
        {
            defaultStorage = new MySQLStorage(new StorageCredentials()
            {
                host = "localhost",
                database = "aventus",
                username = "max",
                password = "pass$1234",
                keepConnectionOpen = true,
            });
            
            Assert.IsTrue(defaultStorage.Connect());
            defaultStorage.Close();
        }

        [Test]
        public async void InitManager()
        {
            bool isRegistered = await DataMainManager.Register(new DataManagerConfig()
            {
                defaultStorage = defaultStorage,
                defaultDM = typeof(DatabaseDMSimple<>)
            });
            Assert.IsTrue(isRegistered);
        }
    }
}
