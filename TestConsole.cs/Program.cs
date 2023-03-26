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
            MySQLStorage storage = new MySQLStorage(new StorageCredentials()
            {
                host = "localhost",
                database = "aventus",
                username = "max",
                password = "pass$1234",
                keepConnectionOpen = true,
            });
            storage.Connect();
            storage.ResetStorage();
            DataMainManager.Register(new DataManagerConfig()
            {
                defaultStorage = storage,
                defaultDM = typeof(DatabaseDMSimple<>),
                log = new DataManagerConfigLog()
                {
                    monitorManagerInit = true,
                }
            }).Wait();

            Console.ReadLine();


            //Person.Create(new Person() { firstname = "Maxime", lastname = "Bétrisey" });

            //new Person()
            //{
            //    firstname = "Test",
            //    lastname = "test"
            //}.Create();


            //Cat felix = new Cat();
            //felix.name = "felix";
            //felix.Create();

            Dog medor = new Dog();
            medor.name = "medor";
            Animal<IAnimal>.Create(medor);
            
            return;

            //Storable<IAnimal>.Create(new List<IAnimal>() { felix, medor });
        }
    }
}
