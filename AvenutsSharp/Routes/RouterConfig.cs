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

        public JsonSerializerSettings? CustomJSONSettings { get; set; }
        public JsonConverter? CustomJSONConverter { get; set; } = new AventusJsonConverter();

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
