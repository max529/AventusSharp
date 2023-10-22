using AventusSharp.Tools;
using AventusSharpTest.Attribute;
using AventusSharpTest.Program.Data.Abstract;
using AventusSharpTest.Test.AAB_Create;
using AventusSharpTest.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharpTest.Test.AAC_Read
{
    [TestFixture]
    [StopOnFail]
    public class GetById
    {

        [Test]
        public void SimpleGetById()
        {
            ResultWithDataError<Cat> cat1 = Cat.GetByIdWithError(1);
            NUnitExt.AssertNoError(cat1);

            Assert.IsTrue(cat1.Result.Equals(Create.felix));

            ResultWithDataError<Cat> cat2 = Cat.GetByIdWithError(2); // its a dog
            NUnitExt.AssertError(cat2);
            Assert.IsNull(cat2.Result);
        }

        [Test]
        public void SimpleGetByIds()
        {
            ResultWithDataError<List<IAnimal>> resultWithError = Animal<IAnimal>.GetByIdsWithError(1, 2);
            NUnitExt.AssertNoError(resultWithError);

            List<IAnimal> animals = resultWithError.Result;
            Assert.IsTrue(animals.Count == 2);
            Assert.IsTrue(animals[0].Equals(AAB_Create.Create.felix));
            Assert.IsTrue(animals[1].Equals(AAB_Create.Create.medor));
        }
    }
}
