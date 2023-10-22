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

namespace AventusSharpTest.Test.AAD_Update
{
    [TestFixture]
    [StopOnFail]
    public class Update
    {
        [Test]
        public void SimpleUpdate()
        {

            Create.felix.name += "2";
            NUnitExt.AssertNoError(Create.felix.UpdateWithError());

            Create.medor.name += "2";
            NUnitExt.AssertNoError(Animal<IAnimal>.UpdateWithError(Create.medor));
        }

        [Test]
        public void UpdateWithExternal()
        {
            Cat c = new()
            {
                Id = 1,
                name = "felix2"
            };
            ResultWithDataError<IAnimal> resultWithError = Animal<IAnimal>.UpdateWithError(c);
            NUnitExt.AssertNoError(resultWithError);

            IAnimal animal = resultWithError.Result;
            Assert.IsTrue(Create.felix.Equals(animal));

        }
    }
    
}
