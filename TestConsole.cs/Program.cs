using AventusSharp.Data;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql;
using AventusSharp.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TestConsole.cs.Data;
using TestConsole.cs.Data.Abstract;
using TestConsole.cs.Logic;

namespace TestConsole.cs
{
    class Program
    {
        static void Main(string[] args)
        {
            MySQLStorage storage = new MySQLStorage(new StorageCredentials(
                host: "localhost",
                database: "aventus",
                username: "max",
                password: "pass$1234"
            )
            {
                keepConnectionOpen = true,
            });
            storage.Connect();
            storage.ResetStorage();
            Task<bool> registeringProcess = DataMainManager.Register(new DataManagerConfig()
            {
                defaultStorage = storage,
                defaultDM = typeof(DatabaseDMSimple<>),
                log = new DataManagerConfigLog()
                {
                    monitorManagerInit = true,
                }
            });

            registeringProcess.Wait();
            if (!registeringProcess.Result)
            {
                Console.WriteLine("something went wrong during loading");
                return;
            }

            WebSocketMiddleware.register();



            //var resultTemp = Animal<IAnimal>.GetQuery()
            //    .Field(animal => animal.id)
            //    .Field(animal => animal.name)
            //    .Where(animal => animal.name == "felix")
            //    .Query();

            //Person.GetQuery().Field(p => p.location.name).Include(p => p.location);
            string name = "felix";
            List<Cat> cats = Cat.Where(a => name == a.name && (a.id == 1));
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

            #region GetAll
            Console.WriteLine("GetAll");
            List<Person> people = Person.GetAll();
            Console.WriteLine("");
            foreach (Person p in people)
            {
                Console.WriteLine("I found person " + p.id + " named " + p.firstname + " " + p.lastname);
            }
            Console.WriteLine("");
            Console.WriteLine("");
            List<IAnimal> animals = Animal<IAnimal>.GetAll();
            foreach (IAnimal a in animals)
            {
                Console.WriteLine("I found " + a.GetType().Name + " " + a.id + " named " + a.name);
            }
            Console.WriteLine("");
            Console.WriteLine("");
            List<IFelin> felins = Felin<IFelin>.GetAll();
            foreach (IFelin f in felins)
            {
                Console.WriteLine("I found " + f.GetType().Name + " " + f.id + " named " + f.name);
            }
            Console.WriteLine("");
            Console.WriteLine("");
            List<Dog> dogs = Dog.GetAll();
            foreach (Dog d in dogs)
            {
                Console.WriteLine("I found a dog " + d.id + " named " + d.name);
            }
            Console.WriteLine("");
            Console.WriteLine("GetAll done");
            #endregion

            #region GetById
            Console.WriteLine("GetById");
            Cat cat1 = Cat.GetById(1);
            Console.WriteLine("the first cat is " + cat1.name);
            Console.WriteLine("GetById done");
            #endregion


            //List<Cat> cats = Cat.Where(a => name == a.name && (a.id == 1));
            foreach (Cat c in cats)
            {
                Console.WriteLine("I found cat with where " + c.id + " named " + c.name);
            }

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
