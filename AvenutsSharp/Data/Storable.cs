using AventusSharp.Attributes;
using AventusSharp.Data.Manager;
using AventusSharp.Data.Storage.Default;
using AvenutsSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace AventusSharp.Data
{
    public class Storable
    {
        internal Dictionary<Type, IStorage> storageByClass = new Dictionary<Type, IStorage>();
    }
    public interface IStorable
    {
        int id { get; set; }
    }

    [ForceInherit]
    public abstract class Storable<T> : IStorable where T : IStorable
    {
        protected Storable() { }

        [Primary, AutoIncrement]
        public int id { get; set; }

        public DateTime createdDate { get; set; }
        public DateTime updatedDate { get; set; }

        public static List<T> GetAll()
        {
            return GenericDM.Get<T>().GetAll<T>();
        }
    }
}
