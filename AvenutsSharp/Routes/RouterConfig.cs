using AventusSharp.Tools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AventusSharp.Routes
{
    public class RouterConfig
    {
        public string ViewDir { get; set; } = Path.Combine(Environment.CurrentDirectory, "Views");

        public string FileUploadTempDir { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

        public Func<string, Dictionary<string, RouterParameterInfo>, Type, Regex>? transformPattern;

        public JsonSerializerSettings? CustomJSONSettings { get; set; }
        public JsonConverter? CustomJSONConverter { get; set; } = new AventusJsonConverter();
    }
}
