using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    internal class DummyDM<U> : GenericDataManager<DummyDM<U>, U> where U : IStorable
    {
        public override List<X> GetAll<X>()
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> Initialize()
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<List<X>> CreateWithError<X>(List<X> values)
        {
            throw new NotImplementedException();
        }
    }
}
