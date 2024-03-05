//using AventusSharp.Data;
//using AventusSharp.Data.Attributes;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace TestConsole.cs.Data.Abstract
//{
//    public interface IAnimal : IStorable
//    {
//        string name { get; set; }

//        bool test { get; set; }
//    }

//    public abstract class Animal<T> : Storable<T>, IAnimal where T : IAnimal
//    {
//        public string name { get; set; }
//        public bool test { get; set; }
//    }

//    public interface IFelin : IAnimal { }
//    public abstract class Felin<T> : Animal<T>, IFelin where T : IFelin
//    {
//        public string color { get; set; }
//    }

//    public class Cat : Felin<Cat>
//    {
//    }
//    public class Dog : Animal<Dog>
//    {

//    }
//}
