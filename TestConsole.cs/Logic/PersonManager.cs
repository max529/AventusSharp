using AventusSharp.Data;
using AventusSharp.Data.Manager;
using AventusSharp.Data.Manager.DB;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using TestConsole.cs.Data;
using TestConsole.cs.Data.Abstract;

namespace TestConsole.cs.Logic
{
    public class PersonManager : DatabaseDM<PersonManager, PersonHuman>
    {

        protected override void DefineShortLinks<X>()
        {
            //ShortLink<PersonHuman>(p => p.location.country);
        }
    }
}
