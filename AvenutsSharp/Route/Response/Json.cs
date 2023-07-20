using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Route.Response
{
    public class Json : IResponse
    {
        private string txt;
        public Json(object? o)
        {
            txt = JsonConvert.SerializeObject(o);
        }

        public Json(object? o, JsonSerializerSettings settings)
        {
            txt = JsonConvert.SerializeObject(o,settings);
        }

        public Json(object? o, JsonConverter converter)
        {
            txt = JsonConvert.SerializeObject(o, converter);
        }

        public async Task send(HttpContext context)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(txt);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }
    }
}
