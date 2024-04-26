using AventusSharp.Data;
using AventusSharp.Tools.Attributes;
using AventusSharp.WebSocket;
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
        [NoTypescript]
        public List<GenericError> Errors { get; }

        public void Print();
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

        public void Print()
        {
            foreach(T error in Errors)
            {
                error.Print();
            }
        }

        /// <summary>
        /// Transform to generic errors
        /// </summary>
        /// <returns></returns>
        public VoidWithError ToGeneric()
        {
            VoidWithError result = new();
            result.Errors = Errors.Select(p => (GenericError)p).ToList();
            return result;
        }
    }

    public class VoidWithError : VoidWithError<GenericError>
    {

    }
    public interface IResultWithError : IWithError
    {
        [NoTypescript]
        public object? Result { get; }
    }
    public class ResultWithError<T, U> : VoidWithError<U>, IResultWithError where U : GenericError
    {
        public T? Result { get; set; } = default;
        object? IResultWithError.Result
        {
            get => Result;
        }

        /// <summary>
        /// Transform to generic errors
        /// </summary>
        /// <returns></returns>
        public ResultWithError<X> ToGeneric<X>(Func<T?, X?> transform)
        {
            ResultWithError<X> result = new();
            result.Errors = Errors.Select(p => (GenericError)p).ToList();
            result.Result = transform(Result);
            return result;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns></returns>
        public new ResultWithError<T> ToGeneric()
        {
            ResultWithError<T> result = new();
            result.Errors = Errors.Select(p => (GenericError)p).ToList();
            result.Result = Result;
            return result;
        }
    }

    public class ResultWithError<T> : ResultWithError<T, GenericError>
    {

    }




}
