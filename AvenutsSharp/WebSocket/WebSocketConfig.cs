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

        public JsonSerializerSettings JSONSettings { get; set; } = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateFormatString = "yyyy-MM-ddTHH:mm:ss.ffffffZ",
            Converters = new List<JsonConverter>() { new AventusJsonConverter() }
        };

        public bool PrintRoute { get; set; } = false;
        public bool PrintTrigger { get; set; } = false;
        public bool PrintQuery { get; set; } = false;
    }
}
