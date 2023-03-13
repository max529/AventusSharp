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
        public void CreateTable(PyramidInfo pyramid);
        public bool TableExist(PyramidInfo pyramid);

        public bool Connect();

        public string GetDatabaseName();
    }


}
