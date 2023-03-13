using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AventusSharp.Log
{
    /// <summary>
    /// Class that manages the error log.
    /// </summary>
    /// <remarks>
    /// Do the same thing as <see cref="LogConsole"/>.<br/>
    /// And write the log into the file error.log without checking if the log is activated.
    /// <para>If the log is activated, the line where the log is written into the file is indicated.</para>
    /// </remarks>
    public class LogError
    {
        private static readonly Mutex mutexInstance = new Mutex();
        private readonly JsonSerializerSettings setting = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private static readonly ConcurrentDictionary<string, LogError> logs = new ConcurrentDictionary<string, LogError>();
        private readonly string filename;

        private bool iswrite = false;

        private LogError(string filename)
        {
            this.filename = filename;
        }

        /// <summary>
        /// Get the singleton for the default file
        /// </summary>
        /// <returns></returns>
        public static LogError getInstance()
        {
            string filename = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            return getInstance(filename);
        }

        /// <summary>
        /// Get the singleton for the file <paramref name="fileName"/>.log
        /// </summary>
        /// <param name="fileName">The name of the file to write the logs</param>
        /// <returns></returns>
        public static LogError getInstance(string fileName)
        {
            if (!logs.ContainsKey(fileName))
            {
                mutexInstance.WaitOne();
                if (!logs.ContainsKey(fileName))
                {
                    if (!logs.TryAdd(fileName, new LogError(fileName)))
                    {
                        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! add false");
                    }
                }
                mutexInstance.ReleaseMutex();
            }

            return logs[fileName];
        }


        /// <summary>
        /// Do the same thing as <see cref="LogConsole.WriteLine(string, string, bool, string, int)"/><br/>
        /// And write the message into the file error.log
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="withMillisec"></param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="memberName">The name of the method that call the method (keep by default)</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void WriteLine(string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = true, [CallerMemberName] string memberName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            long noLine = Log.getInstance(filename).CountLinesLINQ();

            bool logLine = LogConsole.getInstance(filename).ErrorWriteLine(message, logName, log, callerPath, callerNo, withMillisec);

            if (logLine)
            {
                message += "\n\r see " + filename + " at line " + noLine + " for better understanding";
            }
            else
            {
                List<string> caller = callerPath.Split(Path.DirectorySeparatorChar).ToList();

                message += "\n\r  Log : " + caller.Last() + ":" + logName + " is not active";
            }
            string msg = formatError(message, memberName, callerPath, callerNo, withMillisec);
            Log.getInstance("error").WriteLineWithoutFormat(msg);

            iswrite = false;
        }

        /// <summary>
        /// Do the same thing as <see cref="LogConsole.WriteLine(string, string, bool, string, int)"/><br/>
        /// And write the message into the file error.log
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="memberName">The name of the method that call the method (keep by default)</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void WriteLine(string message, [CallerMemberName] string logName = "", bool log = true, [CallerMemberName] string memberName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            WriteLine(message, false, logName, log, memberName, callerPath, callerNo);
        }


        /// <summary>
        /// Do the same thing as <see cref="LogConsole.WriteLine(object, string, bool, string, int)"/><br/>
        /// And write the object in JSON format into the file error.log
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="memberName">The name of the method that call the method (keep by default)</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <param name="withMillisec"></param>
        public void WriteLine(object o, bool withMillisec, string logName, bool log = false, [CallerMemberName] string memberName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            string message = JsonConvert.SerializeObject(o, setting);
            WriteLine(message, withMillisec, logName, log, memberName, callerPath, callerNo);
        }

        /// <summary>
        /// Do the same thing as <see cref="LogConsole.WriteLine(object, string, bool, string, int)"/><br/>
        /// And write the object in JSON format into the file error.log
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="memberName">The name of the method that call the method (keep by default)</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void WriteLine(object o, string logName, bool log = false, [CallerMemberName] string memberName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            WriteLine(o, false, logName, log, memberName, callerPath, callerNo);
        }


        /// <summary>
        /// Pretty print the Exception <paramref name="e"/> with <paramref name="e"/>.Message and <paramref name="e"/>.StackTrace<br/>
        /// If there is an InnerException, it loops into it
        /// </summary>
        /// <param name="e">The exception</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="memberName">The name of the method that call the method (keep by default)</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <param name="withMillisec"></param>
        public void WriteLine(Exception e, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerMemberName] string memberName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            int i = 0;
            do
            {
                if (i > 0)
                {
                    e = e.InnerException;
                }

                long noLine = Log.getInstance(filename).CountLinesLINQ();
                bool logLine = LogConsole.getInstance(filename).ErrorWriteLine("Error" + i + " " + e.GetType().Name + " in " + memberName + " => " + e.Message, logName + "1", log, callerPath, callerNo, withMillisec);
                LogConsole.getInstance(filename).ErrorWriteLine("Stacktrace => " + e.StackTrace, logName, log, callerPath, callerNo, withMillisec);


                string message = "ERROR" + i + " " + e.GetType().Name + " in " + memberName + " => " + e.Message;// "\n\r see " + filename + " at line " + noLine + " for better understanding";

                if (logLine)
                {
                    message += "\n\r see " + filename + " at line " + noLine + " for better understanding";
                }
                else
                {
                    message += "\n\r  Log : " + logName + " is not active";
                }

                string msg = formatError(message, memberName, callerPath, callerNo, withMillisec);
                Log.getInstance("error").WriteLineWithoutFormat(msg);

                message = "Stacktrace => " + e.StackTrace;// + "\n\r see " + filename + " at line " + noLine + " for better understanding";
                if (logLine)
                {
                    message += "\n\r see " + filename + " at line " + noLine + " for better understanding";
                }
                else
                {
                    message += "\n\r  Log : " + logName + " in " + filename + " is not active";
                }
                msg = formatError(message, memberName, callerPath, callerNo, withMillisec);
                Log.getInstance("error").WriteLineWithoutFormat(msg);

                i++;
                //Flush();
            }
            while (e.InnerException != null);
        }

        /// <summary>
        /// Pretty print the Exception <paramref name="e"/> with <paramref name="e"/>.Message and <paramref name="e"/>.StackTrace<br/>
        /// If there is an InnerException, it loops into it
        /// </summary>
        /// <param name="e">The exception</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="memberName">The name of the method that call the method (keep by default)</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void WriteLine(Exception e, [CallerMemberName] string logName = "", bool log = false, [CallerMemberName] string memberName = "", [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            WriteLine(e, false, logName, log, memberName, callerPath, callerNo);
        }



        /// <summary>
        /// Do the same thing as <see cref="LogConsole.Write(string, bool, string, bool, string, int)"/><br/>
        /// And write the message into the file error.log
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <param name="withMillisec"></param>
        public void Write(string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            LogConsole.getInstance(filename).Write(message, withMillisec, logName, log, callerPath, callerNo);
            if (!iswrite)
            {
                string txt = Log.getInstance(filename).formatMsg(message, callerPath, callerNo, logName, withMillisec);
                LogConsole.getInstance(filename).InternalWrite(txt, withMillisec, true, logName, log, callerPath, callerNo);
                Log.getInstance("error").WriteLineWithoutFormat(txt);
                iswrite = true;
            }
            else
            {
                Console.Write(message);
                Log.getInstance(filename).Write(message, logName, log, callerPath, callerNo);
                Log.getInstance("error").WriteLineWithoutFormat(message);

            }
        }

        /// <summary>
        /// Do the same thing as <see cref="LogConsole.Write(string, string, bool, string, int)"/><br/>
        /// And write the message into the file error.log
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        public void Write(string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            Write(message, false, logName, log, callerPath, callerNo);
        }

        private string formatError(string message, string memberName, string callerPath, int callerNo, bool withMillisec)
        {
            DateTime now = DateTime.Now;
            string milliseconds = "";
            if (withMillisec)
            {
                milliseconds += now.Millisecond < 100 ? ".0" : ".";
                milliseconds += now.Millisecond < 10 ? "0" : "";
                milliseconds += now.Millisecond;
            }

            callerPath = callerPath.Split('\\').Last();
            return "[" + now + milliseconds + "] " + callerPath + "." + memberName + "(" + callerNo + "): " + message;
        }

        /// <summary>
        /// Do NOTHING because can cause some problem
        /// </summary>
        /// <remarks>!! TODO try to fix the bugs !!</remarks>
        public void Flush()
        {
            //LogConsole.getInstance(filename).Flush();
            Log.getInstance("error").Flush();
        }
    }
}
