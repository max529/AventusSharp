using AventusSharp.Data.Manager;
using AventusSharp.Data.Manager.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestConsole.cs.Data;
using TestConsole.cs.Data.Abstract;

namespace TestConsole.cs.Logic
{
    public class AnimalManager : DatabaseDM<AnimalManager, IAnimal>
    {
        public override List<Type> defineManualDependances()
        {
            return new List<Type>()
            {
                typeof(PersonHuman)
            };
        }
    }
}
