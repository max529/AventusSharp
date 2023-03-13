namespace AventusSharp.Log.LogConfiguration
{
    internal class DebugLine
    {
        public string name;
        public bool active;
        public int noLine;
        public string outFile;

        public DebugLine(string name, bool active, int noLine, string outFile)
        {
            this.name = name;
            this.active = active;
            this.noLine = noLine;
            this.outFile = outFile;
        }

        public override bool Equals(object obj)
        {
            if (obj is DebugLine line)
            {
                return this == line;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(DebugLine x, DebugLine y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }
            return x.outFile.Equals(y.outFile) && x.name == y.name;
        }

        public static bool operator !=(DebugLine first, DebugLine second)
        {
            return !(first == second);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
