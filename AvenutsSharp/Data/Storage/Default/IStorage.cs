using System;
using System.Collections.Generic;
using System.Text;

namespace AventusSharp.Data.Storage.Default
{
    public interface IStorage
    {
        public bool IsConnectedOneTime { get; }
        public void CreateLinks();
        public void AddPyramid(PyramidInfo pyramid);
        public VoidWithError CreateTable(PyramidInfo pyramid);
        public ResultWithError<bool> TableExist(PyramidInfo pyramid);
        public ResultWithError<List<X>> Create<X>(List<X> values) where X : IStorable;

        public bool Connect();

        public string GetDatabaseName();
    }


}
