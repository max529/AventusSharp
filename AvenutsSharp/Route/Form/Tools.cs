using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AventusSharp.Route.Form
{
    public static class Tools
    {
        public static async Task<object?> ParseBody(HttpContext context, Type type)
        {

            try
            {
                string? contentType = context.Request.ContentType;
                if (!string.IsNullOrEmpty(contentType))
                {

                    // string body = await reader.ReadToEndAsync();
                    if (contentType.StartsWith("multipart/form-data"))
                    {
                        List<IBodyElement> body = new List<IBodyElement>();

                        Dictionary<string, FileBodyElement> filesStreams = new Dictionary<string, FileBodyElement>();
                        StreamingMultipartFormDataParser parser = new StreamingMultipartFormDataParser(context.Request.Body);
                        parser.ParameterHandler += parameter =>
                        {
                            BodyElement b = new BodyElement(parameter.Name, parameter.Data);
                            body.Add(b);
                        };
                        parser.FileHandler += (name, fileName, type, disposition, buffer, bytes, partNumber, additionalProperties) =>
                        {
                            // Write the part of the file we've received to a file stream. (Or do something else)
                            // filestream.Write(buffer, 0, bytes);
                            if (partNumber == 0)
                            {
                                int i = 0;
                                string fileNameToUse = fileName;
                                while (filesStreams.ContainsKey(fileNameToUse))
                                {
                                    List<string> splitted = fileName.Split(".").ToList();
                                    splitted.Insert(splitted.Count - 2, i + "");
                                    i++;
                                    fileNameToUse = string.Join(".", splitted);
                                }
                                if (!filesStreams.ContainsKey(fileNameToUse))
                                {
                                    string tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp");
                                    if (!Directory.Exists(tempFolder))
                                    {
                                        Directory.CreateDirectory(tempFolder);
                                    }
                                    string filePath = Path.Combine(tempFolder, fileNameToUse);
                                    if (File.Exists(filePath))
                                    {
                                        File.Delete(filePath);
                                    }
                                    FileBodyElement fileBody = new FileBodyElement(
                                        name,
                                        type,
                                        new FileStream(filePath, FileMode.Create),
                                        filePath
                                    );
                                    filesStreams.Add(fileNameToUse, fileBody);
                                    body.Add(fileBody);
                                }
                            }

                            filesStreams[fileName].stream.Write(buffer, 0, bytes);

                        };
                        parser.StreamClosedHandler += () =>
                        {
                            foreach (FileBodyElement fileBody in filesStreams.Values)
                            {

                                fileBody.stream.Close();
                                fileBody.stream.Dispose();
                            }
                        };

                        // You can parse synchronously:
                        await parser.RunAsync();

                        string json = "{" + string.Join(",", body) + "}";
                        Console.WriteLine(json);

                        return JsonConvert.DeserializeObject(json, type);
                    }
                    else
                    {
                        Console.WriteLine("Unknow type " + contentType);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return DefaultValue(type);
        }

        public static object? DefaultValue(Type type)
        {
            object? value = null;
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
                value = Activator.CreateInstance(type);
            }
            return value;
        }

    }
}
