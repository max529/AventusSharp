using AventusSharp.Data;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql;
using System;
using System.Collections.Generic;
using TestConsole.cs.Data;
using TestConsole.cs.Data.Abstract;
using TestConsole.cs.Logic;

namespace TestConsole.cs
{
    class Program
    {
        static void Main(string[] args)
        {
            DataMainManager.Register(new DataManagerConfig()
            {
                defaultStorage = new MySQLStorage(new StorageCredentials()
                {
                    host = "localhost",
                    database = "aventus",
                    username = "max",
                    password = "pass$1234",
                    keepConnectionOpen = true,
                }),
                defaultDM = typeof(DatabaseDMSimple<>),
                log = new DataManagerConfigLog()
                {
                    monitorManagerInit = true,
                }
            }).Wait();

            Console.ReadLine();

            // Person.GetAll();
            // Storable<IAnimal>.GetAll();
            // Cat.GetAll();
            // Storable<Cat>.GetAll();

            // AnimalManager.getInstance().GetAll();


            Person.Create(new Person() { firstname = "Maxime", lastname = "Bétrisey" });

            new Person()
            {
                firstname = "Test",
                lastname = "test"
            }.Create();
        }
    }
}
