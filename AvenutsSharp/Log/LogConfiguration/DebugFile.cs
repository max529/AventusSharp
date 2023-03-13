using System.Collections.Generic;

namespace AventusSharp.Log.LogConfiguration
{
    internal class DebugFile
    {
        public string filePath;
        public List<DebugLine> lines;

        public DebugFile(string filePath, List<DebugLine> lines = null)
        {
            this.filePath = filePath;
            this.lines = lines ?? new List<DebugLine>();
        }

        public bool merge(DebugFile debugFile)
        {
            bool merged = false;
            if (this == debugFile)
            {
                foreach (DebugLine lineToAdd in debugFile.lines)
                {
                    if (!lines.Exists(l => l == lineToAdd))
                    {
                        lines.Add(lineToAdd);
                        merged = true;
                    }
                }
            }
            return merged;
        }

        public override bool Equals(object obj)
        {
            if (obj is DebugFile file)
            {
                return this == file;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(DebugFile x, DebugFile y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }
            return x.filePath == y.filePath;
        }

        public static bool operator !=(DebugFile x, DebugFile y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
