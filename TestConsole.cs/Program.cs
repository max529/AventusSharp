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
            #region Creation
            Console.WriteLine("Creation ");

            Person maxime = new Person() { firstname = "Maxime", lastname = "Bétrisey" };
            Person benjamin = new Person() { firstname = "Benjamin", lastname = "Bétrisey" };
            Person.Create(maxime);
            benjamin.Create();


            Cat felix = new Cat();
            felix.name = "felix";
            felix.Create();

            Dog medor = new Dog();
            medor.name = "medor";
            Animal<IAnimal>.Create(medor);

            Cat filou = new Cat();
            filou.name = "filou";

            Dog snoopy = new Dog();
            snoopy.name = "snoopy";

            Storable<IAnimal>.Create(new List<IAnimal>() { filou, snoopy });
            
            Console.WriteLine("Creation done");
            #endregion

            Console.ReadLine();

            #region Update
            Console.WriteLine("Update");
            
            maxime.firstname += "2";
            Person.Update(maxime);

            benjamin.firstname += "2";
            benjamin.Update();


            felix.name += "2";
            felix.Update();
            medor.name += "2";
            Animal<IAnimal>.Update(medor);

            filou.name += "2";
            snoopy.name += "2";
            Storable<IAnimal>.Update(new List<IAnimal>() { filou, snoopy });

            Console.WriteLine("Update done");
            #endregion

            Console.ReadLine();

            #region Delete
            Console.WriteLine("Delete");

            Person.Delete(maxime);
            benjamin.Delete();

            felix.Delete();
            Animal<IAnimal>.Delete(medor);

            Storable<IAnimal>.Delete(new List<IAnimal>() { filou, snoopy });

            Console.WriteLine("Delete done");
            #endregion
        }
    }
}
