using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data
{
    public class ResultWithError<T>
    {
        public bool Success { get => Errors.Count == 0; }

        public List<DataError> Errors = new List<DataError>();
        public T Result { get; set; } = default(T);
    }
    public class VoidWithError
    {
        public bool Success { get => Errors.Count == 0; }

        public List<DataError> Errors = new List<DataError>();
    }
}
