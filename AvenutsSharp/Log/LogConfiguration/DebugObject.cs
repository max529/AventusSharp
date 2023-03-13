namespace AventusSharp.Log.LogConfiguration
{
    internal class DebugObject
    {
        public string name;
        public bool active;

        public DebugObject()
        {

        }

        public DebugObject(string name, bool active)
        {
            this.name = name;
            this.active = active;
        }
    }
}
