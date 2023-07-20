using AventusSharp.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Tools
{
    class NUnitExt
    {
        public static void AssertNoError(IResultWithError result)
        {
            Assert.IsTrue(result.Success, string.Join(", ", result.Errors.Select(e => e.Message)));
        }
        public static void AssertNoError(List<DataError> errors)
        {
            bool isTrue = errors == null || errors.Count == 0;
            Assert.IsTrue(isTrue, string.Join(", ", errors.Select(e => e.Message)));
        }

        public static void AssertError(IResultWithError result)
        {
            Assert.IsTrue(!result.Success);
        }
        public static void AssertError(List<DataError> errors)
        {
            bool isTrue = errors != null && errors.Count > 0;
            Assert.IsTrue(isTrue);
        }
    }
}
