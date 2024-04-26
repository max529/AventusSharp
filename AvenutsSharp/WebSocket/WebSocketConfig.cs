using AventusSharp.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AventusSharp.WebSocket
{
    public class WebSocketConfig
    {

        public string FileUploadTempDir { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

        public Func<string, Dictionary<string, WebSocketRouterParameterInfo>, object, bool, string>? transformPattern;
        public Func<string, bool, Regex>? transformPatternIntoRegex;

        public JsonSerializerSettings? CustomJSONSettings { get; set; }
        public JsonConverter CustomJSONConverter { get; set; } = new AventusJsonConverter();

        public bool PrintRoute { get; set; } = false;
        public bool PrintTrigger { get; set; } = false;
        public bool PrintQuery { get; set; } = false;
    }
}
