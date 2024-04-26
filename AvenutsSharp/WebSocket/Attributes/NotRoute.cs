using System;

namespace AventusSharp.WebSocket.Attributes
{
    /// <summary>
    /// Determine if that the Method isn't a route
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NotRoute : Attribute
    {
       
    }
}
