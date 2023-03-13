using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AventusSharp.Log
{
    /// <summary>
    /// Defined LogLevel for WriteLineWithPrefix
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// NONE
        /// </summary>
        NONE,
        /// <summary>
        /// INFO
        /// </summary>
        INFO,
        /// <summary>
        /// WARNING
        /// </summary>
        WARNING,
        /// <summary>
        /// SUCCESS
        /// </summary>
        SUCCESS,
        /// <summary>
        /// ERROR
        /// </summary>
        ERROR,
    }

    /// <summary>
    /// Class that manages the log and the <see cref="Console"/>
    /// </summary>
    /// <remarks>
    /// Do the same thing as <see cref="Log"/>.<br/>
    /// And write the log into the standard <see cref="Console"/> without checking if the log is activated.
    /// </remarks>
    public class LogConsole
    {
        private static readonly string Reset = "\x1b[0m";

        private static readonly Mutex mutexInstance = new Mutex();
        private static readonly Mutex mutexConsole = new Mutex();
        private readonly JsonSerializerSettings setting = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private static readonly ConcurrentDictionary<string, LogConsole> logs = new ConcurrentDictionary<string, LogConsole>();
        private readonly string filename;

        private bool iswrite = false;


        private LogConsole(string filename)
        {
            this.filename = filename;
        }

        /// <summary>
        /// Get the singleton for the default file
        /// </summary>
        /// <returns></returns>
        public static LogConsole getInstance()
        {
            string filename = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            return getInstance(filename);
        }

        /// <summary>
        /// Get the singleton for the file <paramref name="fileName"/>.log
        /// </summary>
        /// <param name="fileName">The name of the file to write the logs</param>
        /// <returns></returns>
        public static LogConsole getInstance(string fileName)
        {
            if (!logs.ContainsKey(fileName))
            {
                mutexInstance.WaitOne();
                if (!logs.ContainsKey(fileName))
                {
                    if (!logs.TryAdd(fileName, new LogConsole(fileName)))
                    {
                        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! add false");
                    }
                }
                mutexInstance.ReleaseMutex();
            }
            return logs[fileName];
        }


        internal bool ErrorWriteLine(string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0, bool withMillisec = false)
        {
            return WriteLineWithPrefix(LogLevel.ERROR, message, withMillisec, logName, log, callerPath, callerNo);
        }


        internal void InternalWrite(string message, bool withMillisec, bool fromLogError = false, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            mutexConsole.WaitOne();

            if (fromLogError)
            {
                Console.Write(message);
                Log.getInstance(filename).Write(message, logName, log, callerPath, callerNo);
            }
            else
            {
                if (!iswrite)
                {
                    string txt = Log.getInstance(filename).formatMsg(message, callerPath, callerNo, logName, withMillisec);
                    Console.Write(txt);
                    Log.getInstance(filename).Write(message, logName, log, callerPath, callerNo);
                    iswrite = true;
                }
                else
                {
                    Console.Write(message);
                    Log.getInstance(filename).Write(message, logName, log, callerPath, callerNo);
                }
            }
            mutexConsole.ReleaseMutex();
        }

        /// <summary>
        /// Do the same thing as <see cref="Log.Write(string, string, bool, string, int)"/><br/>
        /// And write the message into the standard Console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void Write(string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            InternalWrite(message, withMillisec, false, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="Log.Write(string, string, bool, string, int)"/><br/>
        /// And write the message into the standard Console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void Write(string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            Write(message, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="Log.WriteLine(string, bool, string, bool, string, int)"/><br/>
        /// <!--<inheritdoc cref="Log.WriteLine(string, string, bool, string, int)"/> -->
        /// And write the message into the standard Console with return carrier
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <param name="withMillisec"></param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            bool logLine = Log.getInstance(filename).WriteLine(message, withMillisec, logName, log, callerPath, callerNo);
            string txt = Log.getInstance(filename).formatMsg(message, callerPath, callerNo, logName, withMillisec) + "\r\n";

            mutexConsole.WaitOne();
            if (iswrite)
            {
                Console.WriteLine(message + "\r\n");
                iswrite = false;
            }
            else
            {
                Console.WriteLine(txt);
            }
            mutexConsole.ReleaseMutex();

            return logLine;
        }

        /// <summary>
        /// Do the same thing as <see cref="Log.WriteLine(string, string, bool, string, int)"/><br/>
        /// <!--<inheritdoc cref="Log.WriteLine(string, string, bool, string, int)"/> -->
        /// And write the message into the standard Console with return carrier
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLine(message, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="Log.WriteLine(object, bool, string, bool, string, int)"/><br/>
        /// And write the object in JSON format into the standard Console with return carrier
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(object o, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            string msg = JsonConvert.SerializeObject(o, setting);
            return WriteLine(msg, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="Log.WriteLine(object, string, bool, string, int)"/><br/>
        /// And write the object in JSON format into the standard Console with return carrier
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(object o, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLine(o, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLine(string, bool, string, bool, string, int)"/> but the message is preceded by <paramref name="prefix"/>
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefix(string prefix, string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            Color color;
            switch (prefix.ToLower())
            {
                case "info":
                    color = Color.Cyan;
                    break;
                case "error":
                    color = Color.Red;
                    break;
                case "success":
                    color = Color.Green;
                    break;
                case "warning":
                    color = Color.Yellow;
                    break;
                default:
                    color = Color.None;
                    break;
            }
            return WriteLineWithPrefixColor(prefix, message, color, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLine(string, string, bool, string, int)"/> but the message is preceded by <paramref name="prefix"/>
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefix(string prefix, string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefix(prefix, message, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLine(object, bool, string, bool, string, int)"/> but the message is preceded by <paramref name="prefix"/>
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="o">The object</param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefix(string prefix, object o, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            string msg = JsonConvert.SerializeObject(o, setting);
            return WriteLineWithPrefix(prefix, msg, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLine(object, string, bool, string, int)"/> but the message is preceded by <paramref name="prefix"/>
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="o">The object</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefix(string prefix, object o, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefix(prefix, o, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLine(string, bool, string, bool, string, int)"/> but the message is preceded by <paramref name="logLevel"/> with predefined color for each <paramref name="logLevel"/>
        /// </summary>
        /// <param name="logLevel">The predefined level</param>
        /// <param name="message">The message</param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        /// <remarks>
        /// Color for <see cref="LogLevel.ERROR"/> is <see cref="Color.Red"/><br/>
        /// Color for <see cref="LogLevel.INFO"/> is <see cref="Color.Cyan"/><br/>
        /// Color for <see cref="LogLevel.SUCCESS"/> is <see cref="Color.Green"/><br/>
        /// Color for <see cref="LogLevel.WARNING"/> is <see cref="Color.Yellow"/>
        /// </remarks>
        public bool WriteLineWithPrefix(LogLevel logLevel, string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            Color color;
            switch (logLevel)
            {
                case LogLevel.INFO:
                    color = Color.Cyan;
                    break;
                case LogLevel.ERROR:
                    color = Color.Red;
                    break;
                case LogLevel.SUCCESS:
                    color = Color.Green;
                    break;
                case LogLevel.WARNING:
                    color = Color.Yellow;
                    break;
                case LogLevel.NONE:
                    color = Color.None;
                    break;
                default:
                    color = Color.None;
                    break;
            }
            return WriteLineWithPrefixColor(logLevel.ToString(), message, color, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLine(string, string, bool, string, int)"/> but the message is preceded by <paramref name="logLevel"/> with predefined color for each <paramref name="logLevel"/>
        /// </summary>
        /// <param name="logLevel">The predefined level</param>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        /// <remarks>
        /// Color for <see cref="LogLevel.ERROR"/> is <see cref="Color.Red"/><br/>
        /// Color for <see cref="LogLevel.INFO"/> is <see cref="Color.Cyan"/><br/>
        /// Color for <see cref="LogLevel.SUCCESS"/> is <see cref="Color.Green"/><br/>
        /// Color for <see cref="LogLevel.WARNING"/> is <see cref="Color.Yellow"/>
        /// </remarks>
        public bool WriteLineWithPrefix(LogLevel logLevel, string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefix(logLevel, message, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLine(string, bool, string, bool, string, int)"/> but the message is preceded by <paramref name="logLevel"/> with predefined color for each <paramref name="logLevel"/>
        /// </summary>
        /// <param name="logLevel">The predefined level</param>
        /// <param name="o">The object</param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        /// <remarks>
        /// Color for <see cref="LogLevel.ERROR"/> is <see cref="Color.Red"/><br/>
        /// Color for <see cref="LogLevel.INFO"/> is <see cref="Color.Cyan"/><br/>
        /// Color for <see cref="LogLevel.SUCCESS"/> is <see cref="Color.Green"/><br/>
        /// Color for <see cref="LogLevel.WARNING"/> is <see cref="Color.Yellow"/>
        /// </remarks>
        public bool WriteLineWithPrefix(LogLevel logLevel, object o, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            string msg = JsonConvert.SerializeObject(o, setting);
            return WriteLineWithPrefix(logLevel, msg, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLine(string, string, bool, string, int)"/> but the message is preceded by <paramref name="logLevel"/> with predefined color for each <paramref name="logLevel"/>
        /// </summary>
        /// <param name="logLevel">The predefined level</param>
        /// <param name="o">The object</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        /// <remarks>
        /// Color for <see cref="LogLevel.ERROR"/> is <see cref="Color.Red"/><br/>
        /// Color for <see cref="LogLevel.INFO"/> is <see cref="Color.Cyan"/><br/>
        /// Color for <see cref="LogLevel.SUCCESS"/> is <see cref="Color.Green"/><br/>
        /// Color for <see cref="LogLevel.WARNING"/> is <see cref="Color.Yellow"/>
        /// </remarks>
        public bool WriteLineWithPrefix(LogLevel logLevel, object o, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefix(logLevel, o, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLineWithPrefix(string, string, bool, string, bool, string, int)"/> but the color is user defined
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="color"></param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefixColor(string prefix, string message, Color color, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefixColor(prefix, message, (int)color, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLineWithPrefix(string, string, string, bool, string, int)"/> but the color is user defined
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="color"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefixColor(string prefix, string message, Color color, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefixColor(prefix, message, color, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLineWithPrefix(string, string, bool, string, bool, string, int)"/> but the color is user defined
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="RGB"></param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefixColor(string prefix, string message, int RGB, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            byte R = (byte)((RGB & 0xff0000) >> 16);
            byte G = (byte)((RGB & 0xff00) >> 8);
            byte B = (byte)(RGB & 0xff);

            return WriteLineWithPrefixColor(prefix, message, R, G, B, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLineWithPrefix(string, string, string, bool, string, int)"/> but the color is user defined
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="RGB"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefixColor(string prefix, string message, int RGB, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefixColor(prefix, message, RGB, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="WriteLineWithPrefix(string, string, bool, string, bool, string, int)"/> but the color is user defined
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefixColor(string prefix, string message, byte R, byte G, byte B, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            bool logLine = Log.getInstance(filename).WriteLineWithPrefix(prefix, message, withMillisec, logName, log, callerPath, callerNo);
            string txt = Log.getInstance(filename).formatMsg(message, callerPath, callerNo, logName, withMillisec) + "\r\n";

            if (R > 255)
            {
                R = 255;
            }

            if (G > 255)
            {
                G = 255;
            }

            if (B > 255)
            {
                B = 255;
            }
            string color = "\u001B[38;2;" + R + ";" + G + ";" + B + "m";

            mutexConsole.WaitOne();
            WriteColor(prefix + ": ", color);
            if (iswrite)
            {
                Console.WriteLine(message);
                iswrite = false;
            }
            else
            {
                Console.WriteLine(txt);
            }
            mutexConsole.ReleaseMutex();
            return logLine;
        }

        /// <summary>
        /// Do the same thing as <see cref="WriteLineWithPrefix(string, string, string, bool, string, int)"/> but the color is user defined
        /// </summary>
        /// <param name="prefix">The text that precedes the message</param>
        /// <param name="message">The message</param>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLineWithPrefixColor(string prefix, string message, byte R, byte G, byte B, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLineWithPrefixColor(prefix, message, R, G, B, false, logName, log, callerPath, callerNo);
        }


        private static void WriteColor(string txt, string color)
        {
            //ColorExtension.enableColor();
            Console.Write(color + txt + Reset);
        }

        /// <summary>
        /// Do the same thing as <see cref="Log.Flush"/>
        /// </summary>
        public void Flush()
        {
            Log.getInstance(filename).Flush();
        }
    }
}
