//using AventusSharp.Data;
//using AventusSharp.Data.Attributes;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Nullable = AventusSharp.Data.Attributes.Nullable;

//namespace TestConsole.cs.Data
//{
//    public class Desktop : Storable<Desktop>
//    {
//        public string Name { get; set; } = "";

//        [ReverseLink, AutoRead, AutoCreate, AutoUpdate, AutoDelete]
//        public List<DesktopPosition> Positions { get; set; }
//    }

//    public class DesktopPosition : Storable<DesktopPosition>
//    {
//        [ForeignKey<Desktop>(), DeleteOnCascade]
//        public int DesktopId { get; set; }

//        public int Position { get; set; }

//        [ForeignKey<Application>()]
//        public int ApplicationId { get; set; }
//    }

//    public class Application : Storable<Application>
//    {
//        public string Name { get; set; } = "";
//    }
//}
