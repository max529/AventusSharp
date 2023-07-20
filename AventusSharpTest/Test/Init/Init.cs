using AventusSharp.Data;
using AventusSharp.Data.Manager;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql;
using AventusSharpTest.Attribute;
using AventusSharpTest.Program.Data.Abstract;
using AventusSharpTest.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Test.AAA_Init
{
    [TestFixture]
    [StopOnFail]
    public class Init
    {
        public static MySQLStorage storage;

        [Test]
        [Order(1)]
        public void TestConnection()
        {
            storage = new(new StorageCredentials(
                host: "localhost",
                database: "aventus",
                username: "maxime",
                password: "pass$1234"
            )
            {
                keepConnectionOpen = true,
            });
            NUnitExt.AssertNoError(storage.ConnectWithError());
        }

        [Test]
        [Order(2)]
        public void ResetStorage()
        {
            NUnitExt.AssertNoError(storage.ResetStorage());
        }

        [Test]
        [Order(3)]
        public void InitManager()
        {
            Task<bool> registeringProcess = DataMainManager.Configure(new DataManagerConfig()
            {
                defaultStorage = storage,
                defaultDM = typeof(DatabaseDM<>),
                log = new DataManagerConfigLog()
                {
                    monitorManagerInit = true,
                },
                preferLocalCache = true,
                preferShortLink = false,
                nullByDefault = false,
                searchingAssemblies = new() { Assembly.GetExecutingAssembly() }
            });
            registeringProcess.Wait();
            Assert.IsTrue(registeringProcess.Result, "Something went wrong during loading");
            GenericDM.Get(typeof(IAnimal));
        }
    }
}
