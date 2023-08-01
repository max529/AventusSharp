using AventusSharp.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data
{
    public interface IResultWithError
    {
        public List<IGenericError> Errors { get; }
        public object? Result { get; }
    }
    public interface IResultWithError<T> : IResultWithError where T : IGenericError
    {
        public bool Success { get => Errors.Count == 0; }

        public new List<T> Errors { get; set; }
        
    }
    public class ResultWithError<T, U> : IResultWithError<T> where T : IGenericError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<T> Errors { get; set; } = new List<T>();
        public U? Result { get; set; } = default;
        object? IResultWithError.Result { get { return Result; } }
        List<IGenericError> IResultWithError.Errors
        {
            get
            {
                List<IGenericError> result = new List<IGenericError>();
                foreach(T error in Errors)
                {
                    result.Add(error);
                }
                return result;
            }
        }
    }
    public class ResultWithError<T> : ResultWithError<DataError, T>
    {

    }
    public class VoidWithError<T> where T : IGenericError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<T> Errors = new();
    }

    public class VoidWithError : VoidWithError<DataError>
    {

    }
}
