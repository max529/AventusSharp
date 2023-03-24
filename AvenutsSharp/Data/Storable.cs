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

        #region create
        /**
         * Create inside the DM a bunch of elements and return them
         * If something went wrong an empty list will be returned
         */
        public static List<T> Create(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().Create(values);
            }
            return new List<T>();
        }
        /**
         * Create inside the DM a bunch of elements and return them
         */
        public static ResultWithError<List<T>> CreateWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().CreateWithError(values);
            }

            ResultWithError<List<T>> result = new ResultWithError<List<T>>();
            result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide values to create"));
            result.Result = new List<T>();
            return result;
        }
        /**
         * Create the value inside the DM and return it
         * If something went wrong a null is returned
         */
        public static T Create(T value)
        {
            if (value != null)
            {
                return GenericDM.Get<T>().Create(value);
            }
            return default;
        }
        /**
         * Create the value inside the DM and return it
         */
        public static ResultWithError<T> CreateWithError(T value)
        {
            return GenericDM.Get<T>().CreateWithError(value);
        }
        /**
         * Create the current element inside the DM
         */
        public bool Create()
        {
            Storable<T> result = GenericDM.Get<T>().Create(this);
            if (Equals(result, this))
            {
                return true;
            }
            return false;
        }
        /**
         * Create the current element inside the DM
         * If return Count == 0 it means no error and your item is stored
         */
        public List<DataError> CreateWithError()
        {
            ResultWithError<Storable<T>> result = GenericDM.Get<T>().CreateWithError(this);
            return result.Errors;
        }
        #endregion
    }
}
