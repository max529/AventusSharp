using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    internal class DummyDM<U> : GenericDM<DummyDM<U>, U> where U : IStorable
    {
        public override ResultWithError<List<X>> CreateWithError<X>(List<X> values)
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<List<X>> DeleteWithError<X>(List<X> values)
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<List<X>> GetAllWithError<X>()
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<X> GetByIdWithError<X>(int id)
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<List<X>> GetByIdsWithError<X>(List<int> ids)
        {
            throw new NotImplementedException();
        }


        public override IQueryBuilder<X> CreateQuery<X>()
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<List<X>> UpdateWithError<X>(List<X> values)
        {
            throw new NotImplementedException();
        }

        public override ResultWithError<List<X>> WhereWithError<X>(Expression<Func<X, bool>> func)
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> Initialize()
        {
            throw new NotImplementedException();
        }

        public override IUpdateBuilder<X> CreateUpdate<X>()
        {
            throw new NotImplementedException();
        }

        public override IDeleteBuilder<X> CreateDelete<X>()
        {
            throw new NotImplementedException();
        }
    }
}
