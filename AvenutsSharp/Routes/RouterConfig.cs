using AventusSharp.Routes.Response;
using AventusSharp.Tools;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AventusSharp.Routes
{
    public class RouterConfig
    {
        public Func<HttpContext, IRoute?, string> ViewDir { get; set; }

        public string FileUploadTempDir { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");

        public Func<string, Dictionary<string, RouterParameterInfo>, Type, MethodInfo, Regex>? transformPattern;

        public JsonSerializerSettings JSONSettings { get; set; } = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ",
            Converters = new List<JsonConverter>() { new AventusJsonConverter() }
        };

        public bool PrintRoute { get; set; } = false;
        public bool PrintTrigger { get; set; } = false;


        public RouterConfig()
        {
            ViewDir = (HttpContext context, IRoute? from) =>
            {
                return Path.Combine(Environment.CurrentDirectory, "Views");
            };
        }
    }
}
