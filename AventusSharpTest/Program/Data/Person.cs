using AventusSharp.Data;
using AventusSharp.Data.Attributes;

namespace AventusSharpTest.Program.Data
{

    public class PersonHuman : Storable<PersonHuman>
    {
        public string firstname { get; set; }

        public string lastname { get; set; }

        [Nullable]
        public Location location { get; set; }

        [NotInDB]
        public Role role { get; set; }
    }

    
}
