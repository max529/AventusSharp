using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.Tools
{
    public class GenericError<T> where T : struct, IConvertible
    {
        public T Code { get; set; }

        public string Message { get; set; }

        public List<object> Details { get; set; } = new List<object>();

        public string File { get; set; } = "";

        public int Line { get; set; } = -1;

        public GenericError(T code, string message, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            Code = code;
            Message = message;
            File = callerPath;
            Line = callerNo;
        }

        protected virtual string GetMessageException(bool showDetails)
        {
            string exceptionMsg = "Error " + Code + ": " + Message;
            if (Details.Count > 0)
            {
                exceptionMsg += "\n" + string.Join("\n", Details.Select(d => d.ToString()));
            }
            if (!string.IsNullOrEmpty(File) && Line != -1)
            {
                exceptionMsg += "\n at " + File + ":" + Line;
            }
            return exceptionMsg;
        }
        protected virtual string GetMessagePrint()
        {
            string callerPath = File.Split('\\').Last();
            DateTime now = DateTime.Now;
            return "[" + now + "] " + callerPath + ":" + Line + " => " + Message;
        }
        public Exception GetException()
        {
            return GetException(true);
        }
        public Exception GetException(bool showDetails)
        {
            return new Exception(GetMessageException(showDetails));
        }

        public void Print()
        {
            Console.WriteLine(GetMessagePrint());
        }


    }

    public class GenericErrorExtendable<T> where T : IEnumBaseType
    {
        public T Code { get; set; }

        public string Message { get; set; }

        public List<object> Details { get; set; } = new List<object>();

        public string File { get; set; } = "";

        public int Line { get; set; } = -1;

        public GenericErrorExtendable(T code, string message, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            Code = code;
            Message = message;
            File = callerPath;
            Line = callerNo;
        }

        protected virtual string GetMessageException(bool showDetails)
        {
            string exceptionMsg = "Error " + Code + ": " + Message;
            if (Details.Count > 0)
            {
                exceptionMsg += "\n" + string.Join("\n", Details.Select(d => d.ToString()));
            }
            if (!string.IsNullOrEmpty(File) && Line != -1)
            {
                exceptionMsg += "\n at " + File + ":" + Line;
            }
            return exceptionMsg;
        }
        protected virtual string GetMessagePrint()
        {
            string callerPath = File.Split('\\').Last();
            DateTime now = DateTime.Now;
            return "[" + now + "] " + callerPath + ":" + Line + " => " + Message;
        }
        public Exception GetException()
        {
            return GetException(true);
        }
        public Exception GetException(bool showDetails)
        {
            return new Exception(GetMessageException(showDetails));
        }

        public void Print()
        {
            Console.WriteLine(GetMessagePrint());
        }


    }
    public interface IEnumBaseType { }
    public abstract class EnumBaseType<T> : IEnumBaseType where T : EnumBaseType<T>
    {
        protected readonly static List<T> enumValues = new();

        public readonly int Key;
        public readonly string Value;

        protected EnumBaseType(int key, string value)
        {
            Key = key;
            Value = value;
            enumValues.Add((T)this);
        }

        protected static ReadOnlyCollection<T> GetBaseValues()
        {
            return enumValues.AsReadOnly();
        }

        protected static T? GetBaseByKey(int key)
        {
            foreach (T t in enumValues)
            {
                if (t.Key == key) return t;
            }
            return null;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
