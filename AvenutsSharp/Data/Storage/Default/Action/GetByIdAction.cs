﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class GetByIdAction<T> : GenericAction<T> where T : IStorage
    {
        public abstract ResultWithError<X> run<X>(TableInfo table, int id) where X : IStorable;
    }
}