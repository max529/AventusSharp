using System.Collections.Generic;

namespace AventusSharp.Log.LogConfiguration
{
    internal class DebugRoot
    {
        public List<DebugApp> Library { get; set; } = new List<DebugApp>();
        public List<DebugApp> Service { get; set; } = new List<DebugApp>();
        public List<DebugApp> WebApp { get; set; } = new List<DebugApp>();
        public List<DebugApp> Others { get; set; } = new List<DebugApp>();

        public DebugRoot()
        {
            Library = new List<DebugApp>();
            Service = new List<DebugApp>();
            WebApp = new List<DebugApp>();
            Others = new List<DebugApp>();
        }

        public override bool Equals(object obj)
        {
            if (obj is DebugRoot objRoot)
            {
                return objRoot == this;
            }
            return false;
        }

        public static bool operator ==(DebugRoot left, DebugRoot right)
        {
            if (left is null || right is null)
            {
                return left is null && right is null;
            }
            return true;
        }

        public static bool operator !=(DebugRoot left, DebugRoot right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        public void setList(string app, List<DebugApp> list)
        {
            if (app.Equals("Service"))
            {
                Service = list;
            }
            else if (app.Equals("Library"))
            {
                Library = list;
            }
            else if (app.Equals("WebApp"))
            {
                WebApp = list;
            }
            else
            {
                Others = list;
            }
        }

        public List<DebugApp> getList(string app)
        {
            if (app.Equals("Service"))
            {
                return Service;
            }
            else if (app.Equals("Library"))
            {
                return Library;
            }
            else if (app.Equals("WebApp"))
            {
                return WebApp;
            }
            else
            {
                return Others;
            }
        }
    }
}
