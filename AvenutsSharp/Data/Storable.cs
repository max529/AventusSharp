using AventusSharp.Attributes.Data;
using AventusSharp.Data.Attributes;
using AventusSharp.Data.Manager;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Tools;
using AventusSharp.Tools.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AventusSharp.Data
{
    public class Storable
    {
        public static readonly string Id = "Id";
        internal Dictionary<Type, IDBStorage> storageByClass = new();
    }
    public interface IStorable
    {
        int Id { get; set; }

        public List<string> IsValid(StorableAction action);

        bool Create();
        public List<DataError> CreateWithError();

        bool Update();
        public List<DataError> UpdateWithError();

        public bool Delete();
        public List<DataError> DeleteWithError();
    }

    [ForceInherit]
    [NoTypescript]
    public abstract class Storable<T> : IStorable where T : IStorable
    {
        protected Storable() { }

        [Primary, AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public static List<T> GetAll()
        {
            return GenericDM.Get<T>().GetAll<T>();
        }
        public static ResultWithDataError<List<T>> GetAllWithError()
        {
            return GenericDM.Get<T>().GetAllWithError<T>();
        }
        public static IQueryBuilder<T> StartQuery()
        {
            return GenericDM.Get<T>().CreateQuery<T>();
        }
        public static IUpdateBuilder<T> StartUpdate()
        {
            return GenericDM.Get<T>().CreateUpdate<T>();
        }
        public static IDeleteBuilder<T> StartDelete()
        {
            return GenericDM.Get<T>().CreateDelete<T>();
        }
        public static IExistBuilder<T> StartExist()
        {
            return GenericDM.Get<T>().CreateExist<T>();
        }

        public static T? GetById(int id)
        {
            return GenericDM.Get<T>().GetById<T>(id);
        }
        public static ResultWithDataError<T> GetByIdWithError(int id)
        {
            return GenericDM.Get<T>().GetByIdWithError<T>(id);
        }
        public static List<T> GetByIds(List<int> ids)
        {
            return GenericDM.Get<T>().GetByIds<T>(ids);
        }
        public static ResultWithDataError<List<T>> GetByIdsWithError(List<int> ids)
        {
            return GenericDM.Get<T>().GetByIdsWithError<T>(ids);
        }
        public static ResultWithDataError<List<T>> GetByIdsWithError(params int[] ids)
        {
            return GenericDM.Get<T>().GetByIdsWithError<T>(ids.ToList());
        }

        public static List<T> Where(Expression<Func<T, bool>> func)
        {
            return GenericDM.Get<T>().Where(func);
        }
        public static ResultWithDataError<List<T>> WhereWithError(Expression<Func<T, bool>> func)
        {
            return GenericDM.Get<T>().WhereWithError(func);
        }

        public static bool Exist(Expression<Func<T, bool>> func)
        {
            return GenericDM.Get<T>().Exist(func);
        }
        public static ResultWithDataError<bool> ExistWithError(Expression<Func<T, bool>> func)
        {
            return GenericDM.Get<T>().ExistWithError(func);
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
        public static ResultWithDataError<List<T>> CreateWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().CreateWithError(values);
            }

            ResultWithDataError<List<T>> result = new();
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
        public static ResultWithDataError<T> CreateWithError(T value)
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
                ResultWithDataError<T> result = GenericDM.Get<T>().CreateWithError(TThis);
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
        public static ResultWithDataError<List<T>> UpdateWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().UpdateWithError(values);
            }

            ResultWithDataError<List<T>> result = new();
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
        public static ResultWithDataError<T> UpdateWithError(T value)
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
                ResultWithDataError<T> result = GenericDM.Get<T>().UpdateWithError(TThis);
                if (result.Success)
                {
                    if (Equals(result.Result, this))
                    {
                        return result.Errors;
                    }
                    return new List<DataError>() { new DataError(DataErrorCode.UnknowError, "Element is overrided => impossible") };
                }
                return result.Errors;
            }
            string errorMsg = "Element " + this.GetType() + " isn't a " + typeof(T).Name + ". This should be impossible";
            DataError error = new(DataErrorCode.WrongType, errorMsg);
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
        /// If something went wrong an empty list will be returned
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static List<T> Delete(List<int> ids)
        {
            if (ids != null && ids.Count > 0)
            {
                ResultWithDataError<List<T>> resultTemp = DeleteWithError(ids);
                if(resultTemp.Result != null)
                {
                    return resultTemp.Result;
                }
            }
            return new List<T>();
        }
        /// <summary>
        /// Delete inside the DM a bunch of elements and return them
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ResultWithDataError<List<T>> DeleteWithError(List<T> values)
        {
            if (values != null && values.Count > 0)
            {
                return GenericDM.Get<T>().DeleteWithError(values);
            }

            ResultWithDataError<List<T>> result = new();
            result.Errors.Add(new DataError(DataErrorCode.NoItemProvided, "You must provide values to Delete"));
            result.Result = new List<T>();
            return result;
        }
        /// <summary>
        /// Delete inside the DM a bunch of elements and return them
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static ResultWithDataError<List<T>> DeleteWithError(List<int> ids)
        {
            if (ids != null && ids.Count > 0)
            {
                ResultWithDataError<List<T>> resultTemp = GenericDM.Get<T>().GetByIdsWithError<T>(ids);
                if (resultTemp.Success && resultTemp.Result != null)
                {
                    return GenericDM.Get<T>().DeleteWithError(resultTemp.Result);
                }
                return resultTemp;
            }

            ResultWithDataError<List<T>> result = new();
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

        public static T? Delete(int id)
        {
            ResultWithDataError<T> resultTemp = DeleteWithError(id);
            if (resultTemp.Success && resultTemp.Result != null)
            {
                return resultTemp.Result;
            }
            return default;
        }

        public static ResultWithDataError<T> DeleteWithError(int id)
        {
            ResultWithDataError<T> resultTemp = GenericDM.Get<T>().GetByIdWithError<T>(id);
            if (resultTemp.Success && resultTemp.Result != null)
            {
                resultTemp.Errors = resultTemp.Result.DeleteWithError();
            }
            return resultTemp;
        }
        /// <summary>
        /// Delete the value inside the DM and return it
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ResultWithDataError<T> DeleteWithError(T value)
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
                ResultWithDataError<T> result = GenericDM.Get<T>().DeleteWithError(TThis);
                return result.Errors;
            }
            string errorMsg = "Element " + this.GetType() + " isn't a " + typeof(T).Name + ". This should be impossible";
            DataError error = new(DataErrorCode.WrongType, errorMsg);
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
