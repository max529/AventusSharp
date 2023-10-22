using AventusSharp.Tools;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AventusSharp.Routes.Request
{
    public class RouterBody
    {
        private Dictionary<string, HttpFile> files = new Dictionary<string, HttpFile>();
        private HttpContext context;
        private JObject data = new JObject();
        public RouterBody(HttpContext context)
        {
            this.context = context;
        }


        internal async Task<VoidWithRouteError> Parse()
        {
            VoidWithRouteError result = new ();
            string? contentType = context.Request.ContentType?.ToLower();
            if (!string.IsNullOrEmpty(contentType))
            {
                if (contentType.StartsWith("multipart/form-data"))
                {
                    return await ParseMultiPartForm();
                }
                if (contentType.StartsWith("application/json"))
                {
                    return await ParseJson();
                }
                result.Errors.Add(new RouteError(RouteErrorCode.FormContentTypeUnknown, "The content type " + contentType + " can't be parsed"));
            }
            else
            {
                result.Errors.Add(new RouteError(RouteErrorCode.FormContentTypeUnknown, "The content type " + contentType + " can't be parsed"));
            }
            return result;
        }

        private async Task<VoidWithRouteError> ParseMultiPartForm()
        {
            VoidWithRouteError result = new();
            try
            {
                Dictionary<string, string> bodyJSON = new Dictionary<string, string>();
                StreamingMultipartFormDataParser parser = new StreamingMultipartFormDataParser(context.Request.Body);
                parser.ParameterHandler += parameter =>
                {
                    bodyJSON[parameter.Name] = parameter.Data;
                };
                parser.FileHandler += (name, fileName, type, disposition, buffer, bytes, partNumber, additionalProperties) =>
                {
                    int i = 0;
                    string fileNameToUse = fileName;
                    while (files.ContainsKey(fileNameToUse))
                    {
                        List<string> splitted = fileName.Split(".").ToList();
                        splitted.Insert(splitted.Count - 2, i + "");
                        i++;
                        fileNameToUse = string.Join(".", splitted);
                    }
                    if (partNumber == 0)
                    {
                        if (!files.ContainsKey(fileNameToUse))
                        {
                            string tempFolder = RouterMiddleware.config.FileUploadTempDir;
                            if (!Directory.Exists(tempFolder))
                            {
                                Directory.CreateDirectory(tempFolder);
                            }
                            string filePath = Path.Combine(tempFolder, fileNameToUse);
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }
                            HttpFile file = new HttpFile(name, filePath, type, new FileStream(filePath, FileMode.Create));
                            files.Add(fileNameToUse, file);
                        }
                    }
                    files[fileNameToUse].stream.Write(buffer, 0, bytes);

                };
                parser.StreamClosedHandler += () =>
                {
                    foreach (HttpFile file in files.Values)
                    {

                        file.stream.Close();
                        file.stream.Dispose();
                    }
                };

                // You can parse synchronously:
                await parser.RunAsync();
                data = JObject.Parse("{" + string.Join(",", bodyJSON.Select(p => "\"" + p.Key + "\":\"" + p.Value + "\"")) + "}");

            }
            catch (Exception e)
            {
                result.Errors.Add(new(RouteErrorCode.UnknowError, e));
            }
            return result;
        }

        private async Task<VoidWithRouteError> ParseJson()
        {
            VoidWithRouteError result = new();
            try
            {
                context.Request.EnableBuffering();
                string jsonString = String.Empty;
                
                context.Request.Body.Position = 0;
                using (var inputStream = new StreamReader(context.Request.Body))
                {
                    jsonString = await inputStream.ReadToEndAsync();
                }
                data = JObject.Parse(jsonString);
            }
            catch (Exception e)
            {
                result.Errors.Add(new RouteError(RouteErrorCode.UnknowError, e));
            }
            return result;
        }

        public HttpFile? GetFile()
        {
            if (files.Count > 0)
            {
                return files.Values.ElementAt(0);
            }
            return null;
        }
        public List<HttpFile> GetFiles()
        {
            return files.Values.ToList();
        }

        /// <summary>
        /// Transform data into object T. Add path to tell where to find data to cast
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propPath">Path where to find data</param>
        /// <returns></returns>
        public ResultWithRouteError<object> GetData(Type type, string propPath)
        {
            ResultWithRouteError<object> result = new ();
            //try
            //{
            //    object? temp = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data), new JsonSerializerSettings { 
            //        TypeNameHandling = TypeNameHandling.Objects,
            //        NullValueHandling = NullValueHandling.Ignore,
            //    });
            //    if (type.IsInstanceOfType(temp))
            //    {
            //        result.Result = temp;
            //        return result;
            //    }
            //}
            //catch (Exception e)
            //{
            //    result.Errors.Add(new RouteError(RouteErrorCode.UnknowError, e));
            //}

            try
            {
                JToken? dataToUse = data;
                string[] props = propPath.Split(".");
                foreach (string prop in props)
                {
                    if (!string.IsNullOrEmpty(prop))
                    {
                        dataToUse = dataToUse[prop];
                        if (dataToUse == null)
                        {
                            result.Errors.Add(new RouteError(RouteErrorCode.CantGetValueFromBody, "Can't find path " + propPath + " in your data"));
                            return result;
                        }
                    }
                }
                object? temp = JsonConvert.DeserializeObject(
                    JsonConvert.SerializeObject(dataToUse),
                    type,
                    new JsonSerializerSettings { 
                        TypeNameHandling = TypeNameHandling.Auto, 
                        NullValueHandling = NullValueHandling.Ignore 
                    }
                );
                if (temp != null)
                {
                    result.Result = temp;
                }
            }
            catch (Exception e)
            {
                result.Errors.Add(new RouteError(RouteErrorCode.UnknowError, e));
            }
            return result;
        }

    }

    public class HttpFile
    {
        public string Filename { get; set; }
        public string Filepath { get; set; }
        public string Type { get; set; }

        internal FileStream stream;
        public HttpFile(string filename, string filepath, string type, FileStream stream)
        {
            Filename = filename;
            Filepath = filepath;
            Type = type;
            this.stream = stream;
        }
    }
}
