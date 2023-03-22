using AventusSharp.Data.Storage.Default.Action;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Data.Storage.Mysql.Action
{
    internal class MySQLAction : StorageAction<MySQLStorage>
    {
        public MySQLAction(MySQLStorage Storage) : base(Storage)
        {
        }

        protected override TableExistAction<MySQLStorage> TableExist => new TableExistAction();

        protected override CreateTableAction<MySQLStorage> CreateTable => new CreateTableAction();

        protected override CreateAction<MySQLStorage> Create => new CreateAction();
    }
}
