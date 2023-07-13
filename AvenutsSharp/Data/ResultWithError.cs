using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data
{
    public interface IResultWithError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<DataError> Errors { get; set; }
        public object? Result { get; }
    }
    public class ResultWithError<T> : IResultWithError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<DataError> Errors { get; set; } = new List<DataError>();
        public T? Result { get; set; } = default;
        object? IResultWithError.Result { get { return Result; } }
    }
    public class VoidWithError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<DataError> Errors = new();
    }
}
