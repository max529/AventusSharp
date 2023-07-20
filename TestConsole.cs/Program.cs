﻿using AventusSharp.Data;
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

MySQLStorage storage = new(new StorageCredentials(
                host: "localhost",
                database: "aventus",
                username: "maxime",
                password: "pass$1234"
            )
{
    keepConnectionOpen = true,
});
if (!storage.Connect())
{
    Console.WriteLine("Error during connection");
    return;
}
storage.ResetStorage();

DataMainManager.Configure(config =>
{
    config.defaultStorage = storage;
    config.defaultDM = typeof(DatabaseDM<>);
    config.log = new DataManagerConfigLog()
    {
        monitorManagerInit = true,
    };
    config.preferLocalCache = true;
    config.preferShortLink = true;
    config.nullByDefault = false;
});

Task<VoidWithError> registeringProcess = DataMainManager.Init();

registeringProcess.Wait();
if (!registeringProcess.Result.Success)
{
    Console.WriteLine("something went wrong during loading");
    return;
}


#region Creation
Console.WriteLine("Creation ");

EuropeanCountry swiss = new()
{
    PIB = 100,
    shortName = "CH"
};
swiss.Create();
// TODO manage auto-create / auto-update / auto-delete
// TODO manage deleteOnCascade / DeleteSetNull
// TODO manage reverse link [Attr]
// TODO manage n-m links
swiss.shortName = "CH2";

Location home = new() { name = "Home", country = swiss };
var resTe = home.CreateWithError();
home.name = "home2";
home.country.shortName = "CH3";
home.Update();

PersonHuman maxime = new() { firstname = "Maxime", lastname = "Bétrisey", location = home };
PersonHuman benjamin = new() { firstname = "Benjamin", lastname = "Bétrisey" };
PersonHuman.Create(maxime);
benjamin.Create();


Cat felix = new()
{
    name = "felix",
    color = "brun"
};
felix.Create();

Dog medor = new()
{
    name = "medor"
};
Animal<IAnimal>.Create(medor);

Cat filou = new()
{
    color = "white",
    name = "filou"
};

Dog snoopy = new()
{
    name = "snoopy"
};

Storable<IAnimal>.Create(new List<IAnimal>() { filou, snoopy });

Console.WriteLine("Creation done");
#endregion

#region GetAll

Console.WriteLine("GetAll");
List<PersonHuman> people = PersonHuman.GetAll();
Console.WriteLine("");
foreach (PersonHuman p in people)
{
    Console.WriteLine(p.location == home);
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

Console.ReadLine();

#region Update
Console.WriteLine("Update");

Cat c = new()
{
    id = 1,
    name = "felix2"
};
var test = Animal<IAnimal>.UpdateWithError(c);

Console.WriteLine(felix.name);

//maxime.firstname += "2";
//PersonHuman.Update(maxime);

//benjamin.firstname += "2";
//benjamin.Update();


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

var temp = home.DeleteWithError();
Console.WriteLine("");

PersonHuman.Delete(maxime);
benjamin.Delete();

felix.Delete();
Animal<IAnimal>.Delete(medor);

Storable<IAnimal>.Delete(new List<IAnimal>() { filou, snoopy });

Console.WriteLine("Delete done");
#endregion