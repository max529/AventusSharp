using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AventusSharp.Routes.Response
{
    public class View : IResponse
    {
        internal static string directory = RouterMiddleware.config.ViewDir;

        private string viewName;
        public View(string viewName)
        {
            this.viewName = viewName;
        }
        public async Task send(HttpContext context)
        {
            string path = Path.Combine(directory, viewName);
            if (!path.EndsWith(".html"))
            {
                path += ".html";
            }
            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path) ?? Array.Empty<byte>();
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html";
                context.Response.Headers.Add("content-length", bytes.Length + "");
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
    }
}
