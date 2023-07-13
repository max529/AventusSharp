using AventusSharp.Attributes;
using AventusSharp.Data.Manager;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using AvenutsSharp.Attributes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace AventusSharp.Data
{
    public class Storable
    {
        internal Dictionary<Type, IDBStorage> storageByClass = new();
    }
    public interface IStorable
    {
#pragma warning disable IDE1006
        int id { get; set; }
#pragma warning restore IDE1006

        public List<string> IsValid(StorableAction action);
    }

    [ForceInherit]
    public abstract class Storable<T> : IStorable where T : IStorable
    {
        protected Storable() { }

        [Primary, AutoIncrement]
        public int id { get; set; }
#pragma warning disable IDE1006
        public DateTime createdDate { get; set; }
        public DateTime updatedDate { get; set; }
#pragma warning restore IDE1006

        public static List<T> GetAll()
        {
            return GenericDM.Get<T>().GetAll<T>();
        }
        public static IQueryBuilder<T>? StartQuery()
        {
            return GenericDM.Get<T>().CreateQuery<T>();
        }
        public static IUpdateBuilder<T>? StartUpdate()
        {
            IUpdateBuilder<T>? result = GenericDM.Get<T>().CreateUpdate<T>();
            return result;
        }
        public static IDeleteBuilder<T>? StartDelete()
        {
            IDeleteBuilder<T>? result = GenericDM.Get<T>().CreateDelete<T>();
            return result;
        }

        public static T GetById(int id)
        {
            return GenericDM.Get<T>().GetById<T>(id);
        }

        public static List<T> Where(Expression<Func<T, bool>> func)
        {
            return GenericDM.Get<T>().Where(func);
        }

        #region Create
        /// <summary>
        /// Create inside the DM a bunch of elements and return them
        /// If something went wrong an empty list will be returned
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<T> Create(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().Create(values);
            }
            return new List<T>();
        }
        /// <summary>
        /// Create inside the DM a bunch of elements and return them
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ResultWithError<List<T>> CreateWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().CreateWithError(values);
            }

            ResultWithError<List<T>> result = new();
            result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide values to create"));
            result.Result = new List<T>();
            return result;
        }
        /// <summary>
        /// Create the value inside the DM and return it
        /// If something went wrong a null is returned
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T? Create(T value)
        {
            if (value != null)
            {
                return GenericDM.Get<T>().Create(value);
            }
            return default;
        }
        /// <summary>
        /// Create the value inside the DM and return it
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ResultWithError<T> CreateWithError(T value)
        {
            return GenericDM.Get<T>().CreateWithError(value);
        }
        /// <summary>
        /// Create the current element inside the DM
        /// </summary>
        /// <returns></returns>
        public bool Create()
        {
            return CreateWithError().Count == 0;
        }
        /// <summary>
        /// Create the current element inside the DM
        /// If return Count == 0 it means no error and your item is stored
        /// </summary>
        /// <returns></returns>
        public List<DataError> CreateWithError()
        {
            if (this is T TThis)
            {
                ResultWithError<T> result = GenericDM.Get<T>().CreateWithError(TThis);
                if (result.Success)
                {
                    if (Equals(result.Result, this))
                    {
                        return new List<DataError>();
                    }
                    return new List<DataError>() { new DataError(DataErrorCode.UnknowError, "Element is overrided => impossible") };
                }
                return result.Errors;

            }
            string errorMsg = "Element " + this.GetType() + " isn't a " + typeof(T).Name + ". This should be impossible";
            DataError error = new(DataErrorCode.WrongType, errorMsg);
            error.Print();
            return new List<DataError>() { error };
        }
        #endregion

        #region Update
        /// <summary>
        /// Update inside the DM a bunch of elements and return them
        /// If something went wrong an empty list will be returned
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<T> Update(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().Update(values);
            }
            return new List<T>();
        }
        /// <summary>
        /// Update inside the DM a bunch of elements and return them
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ResultWithError<List<T>> UpdateWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().UpdateWithError(values);
            }

            ResultWithError<List<T>> result = new();
            result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide values to Update"));
            result.Result = new List<T>();
            return result;
        }
        /// <summary>
        /// Update the value inside the DM and return it
        /// If something went wrong a null is returned
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T? Update(T value)
        {
            if (value != null)
            {
                return GenericDM.Get<T>().Update(value);
            }
            return default;
        }
        /// <summary>
        /// Update the value inside the DM and return it
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ResultWithError<T> UpdateWithError(T value)
        {
            return GenericDM.Get<T>().UpdateWithError(value);
        }
        /// <summary>
        /// Update the current element inside the DM
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            return UpdateWithError().Count == 0;
        }

        /// <summary>
        /// Update the current element inside the DM
        /// If return Count == 0 it means no error and your item is stored
        /// </summary>
        /// <returns></returns>
        public List<DataError> UpdateWithError()
        {
            if (this is T TThis)
            {
                ResultWithError<T> result = GenericDM.Get<T>().UpdateWithError(TThis);
                if (Equals(result.Result, this))
                {
                    return result.Errors;
                }
                return new List<DataError>() { new DataError(DataErrorCode.UnknowError, "Element is overrided => impossible") };
            }
            string errorMsg = "Element " + this.GetType() + " isn't a " + typeof(T).Name + ". This should be impossible";
            DataError error = new(DataErrorCode.WrongType, errorMsg);
            error.Print();
            return new List<DataError>() { error };
        }
        #endregion

        #region Delete
        /// <summary>
        /// Delete inside the DM a bunch of elements and return them
        /// If something went wrong an empty list will be returned
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<T> Delete(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().Delete(values);
            }
            return new List<T>();
        }
        /// <summary>
        /// Delete inside the DM a bunch of elements and return them
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ResultWithError<List<T>> DeleteWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().DeleteWithError(values);
            }

            ResultWithError<List<T>> result = new();
            result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide values to Delete"));
            result.Result = new List<T>();
            return result;
        }
        /// <summary>
        /// Delete the value inside the DM and return it
        /// If something went wrong a null is returned
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T? Delete(T value)
        {
            if (value != null)
            {
                return GenericDM.Get<T>().Delete(value);
            }
            return default;
        }
        /// <summary>
        /// Delete the value inside the DM and return it
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ResultWithError<T> DeleteWithError(T value)
        {
            return GenericDM.Get<T>().DeleteWithError(value);
        }

        /// <summary>
        /// Delete the current element inside the DM
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            return DeleteWithError().Count == 0;
        }
        /// <summary>
        /// Delete the current element inside the DM
        /// If return Count == 0 it means no error and your item is stored
        /// </summary>
        /// <returns></returns>
        public List<DataError> DeleteWithError()
        {
            if (this is T TThis)
            {
                ResultWithError<T> result = GenericDM.Get<T>().DeleteWithError(TThis);
                if (Equals(result.Result, this))
                {
                    return result.Errors;
                }
                return new List<DataError>() { new DataError(DataErrorCode.UnknowError, "Element is overrided => impossible") };
            }
            string errorMsg = "Element " + this.GetType() + " isn't a " + typeof(T).Name + ". This should be impossible";
            DataError error = new(DataErrorCode.WrongType, errorMsg);
            error.Print();
            return new List<DataError>() { error };
        }
        #endregion


        public List<string> IsValid(StorableAction action)
        {
            List<string> errors = new();
            errors.AddRange(ValidationRules(action));
            return errors;
        }

        protected virtual List<string> ValidationRules(StorableAction action)
        {
            return new List<string>();
        }
    }


}
