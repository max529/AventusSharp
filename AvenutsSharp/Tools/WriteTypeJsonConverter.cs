using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AventusSharp.Tools
{
    /// <summary>
    /// Custom converter to add type when we need it (avoid dico and list bc crash in js)
    /// </summary>
    public class WriteTypeJsonConverter : JsonConverter
    {
        private readonly List<string> propToRemove = new() { };

        /// <summary>
        /// always true because we can always convert until object
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }
        /// <summary>
        /// always false because its a writer not a reader
        /// </summary>
        public override bool CanRead
        {
            get { return false; }
        }
        /// <summary>
        /// Throw an error because this class is a writer not a reader
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                return;
            }
            lock (value)
            {
                Type type = value.GetType();
                if (type.IsPrimitive || TypeTools.PrimitiveType.Contains(type))
                {
                    JToken t = JToken.FromObject(value);
                    t.WriteTo(writer);
                }
                else if (type.BaseType != null && type.BaseType == typeof(Enum))
                {
                    JToken t = JToken.FromObject(value);
                    t.WriteTo(writer);
                }
                else if (type.IsGenericType && type.GetInterfaces().Contains(typeof(IDictionary)))
                {
                    IEnumerable? keys = (IEnumerable?)type.GetProperty("Keys")?.GetValue(value, null);
                    IEnumerable? values = (IEnumerable?)type.GetProperty("Values")?.GetValue(value, null);
                    if (keys == null || values == null)
                    {
                        return;
                    }
                    IEnumerator valueEnumerator = values.GetEnumerator();
                    JObject jo = new();
                    foreach (object key in keys)
                    {
                        valueEnumerator.MoveNext();
                        if (valueEnumerator.Current != null)
                        {
                            string? keyStr = key.ToString();
                            if (keyStr != null)
                            {
                                jo.Add(keyStr, JToken.FromObject(valueEnumerator.Current, serializer));
                            }
                        }
                    }
                    jo.WriteTo(writer);
                }
                else if (type.IsGenericType && type.GetInterfaces().Contains(typeof(IList)))
                {
                    IEnumerable values = (IEnumerable)value;
                    IEnumerator valueEnumerator = values.GetEnumerator();
                    JArray jo = new();
                    while (valueEnumerator.MoveNext())
                    {
                        if (valueEnumerator.Current != null)
                        {
                            jo.Add(JToken.FromObject(valueEnumerator.Current, serializer));
                        }

                    }
                    jo.WriteTo(writer);
                }
                else
                {
                    JObject jo = new()
                    {
                        { "$type", type.FullName + ", " + type.Assembly.GetName().Name }
                    };

                    foreach (PropertyInfo prop in type.GetProperties())
                    {
                        if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                        {

                            object? propVal = prop.GetValue(value, null);
                            if (propVal != null)
                            {
                                if (!propToRemove.Contains(prop.Name))
                                {
                                    jo.Add(prop.Name, JToken.FromObject(propVal, serializer));
                                }
                            }
                        }
                    }

                    foreach (FieldInfo prop in type.GetFields())
                    {

                        object? propVal = prop.GetValue(value);
                        if (propVal != null)
                        {
                            if (!propToRemove.Contains(prop.Name))
                            {
                                jo.Add(prop.Name, JToken.FromObject(propVal, serializer));
                            }
                        }

                    }
                    jo.WriteTo(writer);
                }
            }
        }
    }
}
