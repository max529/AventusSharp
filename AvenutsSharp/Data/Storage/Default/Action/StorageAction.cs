using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Default.Action
{
    internal abstract class StorageAction<T> where T : IStorage
    {
        public TableExistAction<T> _TableExist { get; }
        protected abstract TableExistAction<T> TableExist { get; }
        public CreateTableAction<T> _CreateTable { get; }
        protected abstract CreateTableAction<T> CreateTable { get; }

        public StorageAction(T Storage)
        {
            _TableExist = TableExist;
            _TableExist.Storage = Storage;

            _CreateTable = CreateTable;
            _CreateTable.Storage = Storage;
        }
    }
}
