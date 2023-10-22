using AventusSharp.Data;
using AventusSharp.Tools.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Tools
{
    public interface IWithError
    {
        public List<GenericError> Errors { get; }
    }
    public class VoidWithError<T> : IWithError where T : GenericError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<T> Errors { get; set; } = new();
        
        [NoTypescript]
        List<GenericError> IWithError.Errors
        {
            get
            {
                List<GenericError> errors = new List<GenericError>();
                foreach(T error in Errors)
                {
                    errors.Add(error);
                }
                return errors;
            }
        }
    }

    public class VoidWithError : VoidWithError<GenericError>
    {

    }
    public interface IResultWithError : IWithError
    {
        public object? Result { get; }
    }
    public class ResultWithError<T, U> : VoidWithError<U>, IResultWithError where U : GenericError
    {
        public T? Result { get; set; } = default;
        object? IResultWithError.Result
        {
            get => Result;
        }
    }

    public class ResultWithError<T> : ResultWithError<T, GenericError>
    {

    }




}
