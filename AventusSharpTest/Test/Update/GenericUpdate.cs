﻿using AventusSharp.Data;
using AventusSharp.Data.Manager;
using AventusSharpTest.Attribute;
using AventusSharpTest.Program.Data;
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
    public class GenericUpdate
    {
        [Test]
        public void SimpleUpdate()
        {
            Cat c = new Cat()
            {
                color = "Pink"
            };
            int id = 1;
            ResultWithError<List<Cat>> result = Cat.StartUpdate().Field(c => c.color).Where(c => c.id == id).RunWithError(c);
            NUnitExt.AssertNoError(result);

            Assert.IsTrue(result.Result.Count == 1);
            Assert.IsTrue(result.Result[0].id == 1);
            Assert.IsTrue(result.Result[0].color == "Pink");
        }

        [Test]
        public void UpdateWithVariable()
        {
            Cat c = new Cat()
            {
                color = "Orange"
            };
            int id = 1;
            IUpdateBuilder<Cat> query = Cat.StartUpdate().Field(c => c.color).WhereWithParameters(c => c.id == id);
            query.Prepare(3);
            ResultWithError<List<Cat>> result = query.RunWithError(c);
            NUnitExt.AssertNoError(result);

            Assert.IsTrue(result.Result.Count == 1);
            Assert.IsTrue(result.Result[0].id == 3);
            Assert.IsTrue(result.Result[0].color == "Orange");
        }

        [Test]
        public void UpdateLink()
        {
            PersonHuman temp = new()
            {
                location = new()
                {
                    name = "Casa"
                }
            };
            int id = 1;
            ResultWithError<List<PersonHuman>> resultWithError = PersonHuman.StartUpdate()
                .Field(p => p.location.name)
                .Where(c => c.id == id)
                .RunWithError(temp);

            NUnitExt.AssertNoError(resultWithError);

            Assert.IsTrue(resultWithError.Result.Count == 1);
            Assert.IsTrue(resultWithError.Result[0].id == id);
            Assert.IsTrue(resultWithError.Result[0].location.name == "Case");
        }
    }
}
