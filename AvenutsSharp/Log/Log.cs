using AventusSharp.Log.LogConfiguration;
using FluentScheduler;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AventusSharp.Log
{
    /// <summary>
    /// Class that manages the log.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Class that can write some logs into a file.log and read the LogConfiguration for activate/deactivate the logs.<br/>
    /// The logs are written into a folder Logs at the root of the program.
    /// All the logs write into the file are named and can be (de)activated with a configuration found into the folder Logs/LogConfiguration.
    /// The log are backed up at 02:00:00 every day.
    /// If there is a problem while writing, a file LogError.txt can contains the explanation.
    /// All the logs are preceded by some information<br/>
    /// <example> Example of log : <c>[dd.MM.YYYY hh:mm:ss] FileName.cs.logName(LineNumber): Message</c></example>
    /// </para>
    /// </remarks>

    public class Log
    {
        private static readonly ConcurrentDictionary<string, Log> logs = new ConcurrentDictionary<string, Log>();

        private readonly JsonSerializerSettings setting = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private static readonly Mutex mutexFile = new Mutex();
        private static readonly Mutex mutexTxt = new Mutex();
        private static readonly Mutex mutexConfig = new Mutex();
        private static readonly Mutex mutexInstance = new Mutex();
        private static readonly Mutex mutexError = new Mutex();
        private static readonly Mutex mutexTimer = new Mutex();

        private static bool isFirstTime = true;
        private readonly string filename;
        private string logTxt;
        private string writeTxt;
        private System.Timers.Timer timer;
        private readonly string path;
        private int backupNumber = 0;
        internal static readonly string errorLog = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "Logs" + Path.DirectorySeparatorChar + "LogError.txt";

        private Log(string fileName, bool isFirstTime = false)
        {
            filename = fileName;
            logTxt = "";
            writeTxt = "";
            path = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "Logs";

            if (isFirstTime)
            {
                Registry registry = new Registry();
                registry.Schedule(() => doBackup(true)).ToRunEvery(1).Days().At(02, 00);
                JobManager.Initialize(registry);
                doBackup();

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                ConfigurationLog.getInstance();
            }
            ResetFile();

            path = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "Logs" + Path.DirectorySeparatorChar + filename + ".log";
        }

        //public static void Flush(bool all)
        //{
        //    foreach (KeyValuePair<string, Log> log in logs)
        //    {
        //        log.Value.Flush();
        //    }
        //}

        /// <summary>
        /// Get the singleton for the default file
        /// </summary>
        /// <returns></returns>
        public static Log getInstance()
        {
            string filename = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            return getInstance(filename);
        }

        /// <summary>
        /// Get the singleton for the file <paramref name="fileName"/>.log
        /// </summary>
        /// <param name="fileName">The name of the file to write the logs</param>
        /// <returns></returns>
        public static Log getInstance(string fileName)
        {
            fileName = fileName.ToLower();
            if (!logs.ContainsKey(fileName))
            {
                mutexInstance.WaitOne();
                if (!logs.ContainsKey(fileName))
                {
                    if (!logs.TryAdd(fileName, new Log(fileName, isFirstTime)))
                    {
                        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! add false");
                    }
                    if (isFirstTime)
                    {
                        isFirstTime = false;
                    }
                }
                mutexInstance.ReleaseMutex();
            }
            return logs[fileName];
        }

        /// <summary>
        /// Stop the backup scheduler
        /// Should be called on Service.onStop / WebApp.onStopped
        /// </summary>
        public static void stop()
        {
            JobManager.Stop();
        }

        /// <summary>
        /// Flush all the Log
        /// </summary>
        public static void FlushAll()
        {
            foreach (System.Collections.Generic.KeyValuePair<string, Log> log in logs)
            {
                log.Value.Flush();
            }
        }

        internal long CountLinesLINQ()
        {
            //return -1;
            mutexFile.WaitOne();
            long linesTotal = -1;
            try
            {
                if (!File.Exists(path))
                {
                    FileStream fStrm = File.Create(path);
                    fStrm.Close();
                    fStrm.Dispose();
                }
                int count = Regex.Matches(logTxt, "\n").Count;
                int nbLines = File.ReadAllLines(path).Length;
                linesTotal = nbLines + 1 + count;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                writeToErrorFile(e);
            }
            mutexFile.ReleaseMutex();
            return linesTotal;
        }

        internal void WriteLineWithoutFormat(string message)
        {
            mutexTxt.WaitOne();
            logTxt += message + "\r\n";
            writeTxt = "";
            Flush();
            mutexTxt.ReleaseMutex();
        }

        internal bool WriteLineWithPrefix(string prefix, string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            mutexTxt.WaitOne();
            DebugLine debugFile = new DebugLine(logName, log, callerNo, filename);
            if (callerPath == null)
            {
                Console.WriteLine("something is null");
            }
            if (callerNo == 0)
            {
                Console.WriteLine("something is null 2");
            }

            ConfigurationLog.getInstance().setLogToggle(callerPath, debugFile);
            bool logLine = ConfigurationLog.getInstance().getLogToggle(callerPath, debugFile);
            if (logLine)
            {
                logTxt += writeTxt + "\r\n" + prefix + ": " + formatMsg(message, callerPath, callerNo, logName, withMillisec) + "\r\n";
                writeTxt = "";
                EnableTimer();
            }

            mutexTxt.ReleaseMutex();
            return logLine;
        }

        internal bool WriteLineWithPrefix(string prefix, object o, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            string obj = JsonConvert.SerializeObject(o, setting);
            return WriteLineWithPrefix(prefix, obj, withMillisec, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Write the <paramref name="message"/> into the file with the return carrier<br/>
        /// <example>Example of text write into log : <c>[dd.MM.YYYY hh:mm:ss] <paramref name="callerPath"/>.cs.<paramref name="logName"/>(<paramref name="callerNo"/>): <paramref name="message"/></c><br/></example>
        /// <example>Example of text write into log if <paramref name="withMillisec"/> is true: <c>[dd.MM.YYYY hh:mm:ss.msmsms] <paramref name="callerPath"/>.cs.<paramref name="logName"/>(<paramref name="callerNo"/>): <paramref name="message"/></c><br/></example>
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is <see cref="CallerMemberNameAttribute"/>)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <param name="withMillisec">Should the millisec appear into </param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(string message, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            mutexTxt.WaitOne();
            DebugLine debugFile = new DebugLine(logName, log, callerNo, filename);
            if (callerPath == null)
            {
                Console.WriteLine("something is null");
            }
            if (callerNo == 0)
            {
                Console.WriteLine("something is null 2");
            }

            ConfigurationLog.getInstance().setLogToggle(callerPath, debugFile);
            bool logLine = ConfigurationLog.getInstance().getLogToggle(callerPath, debugFile);
            if (logLine)
            {
                logTxt += writeTxt + "\r\n" + formatMsg(message, callerPath, callerNo, logName, withMillisec) + "\r\n";
                writeTxt = "";
                EnableTimer();
            }

            mutexTxt.ReleaseMutex();
            return logLine;
        }

        /// <summary>
        /// Write the <paramref name="message"/> into the file with the return carrier<br/>
        /// <example>Example of text write into log : <c>[dd.MM.YYYY hh:mm:ss] <paramref name="callerPath"/>.cs.<paramref name="logName"/>(<paramref name="callerNo"/>): <paramref name="message"/></c><br/></example>
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is <see cref="CallerMemberNameAttribute"/>)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            return WriteLine(message, false, logName, log, callerPath, callerNo);
        }


        /// <summary>
        /// Write the <paramref name="o"/> in JSON format into the file with the return carrier
        /// </summary>
        /// <param name="o">The object</param>
        /// <param name="logName">The name of the log that can be (de)activated into the LogConfiguration (by default the logName is the name of the method where it is called)</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <param name="callerNo">The line in the file that call the method (keep by default)</param>
        /// <param name="withMillisec"></param>
        /// <returns>Return the active state of <paramref name="logName"/></returns>
        public bool WriteLine(object o, bool withMillisec, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            string obj = JsonConvert.SerializeObject(o, setting);
            return WriteLine(obj, withMillisec, logName, log, callerPath, callerNo);
        }

        /// <summary>
        /// Write the <paramref name="o"/> in JSON format into the file with the return carrier
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
        /// Write the message into file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logName"></param>
        /// <param name="log"></param>
        /// <param name="callerPath"></param>
        /// <param name="callerNo"></param>
        public void Write(string message, [CallerMemberName] string logName = "", bool log = false, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerNo = 0)
        {
            mutexTxt.WaitOne();
            if (string.IsNullOrEmpty(writeTxt))
            {
                writeTxt = formatMsg(message, callerPath, callerNo, logName, false);
            }
            else
            {
                writeTxt += message;
            }
            EnableTimer();
            mutexTxt.ReleaseMutex();
        }

        /// <summary>
        /// Method that can add a <paramref name="logName"/> value into LogConfiguration that can be changed without the need of re-build
        /// </summary>
        /// <param name="logName">The name of the log</param>
        /// <param name="log">The default active state</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        public void setToggle(string logName, bool log = false, [CallerFilePath] string callerPath = "")
        {
            mutexConfig.WaitOne();
            ConfigurationLog.getInstance().setLogObjectToggle(logName, log, callerPath);
            mutexConfig.ReleaseMutex();
        }

        /// <summary>
        /// Method that can retreive the state of <paramref name="logObjectName"/>
        /// </summary>
        /// <param name="logObjectName">The name of the log</param>
        /// <param name="callerPath">The path of the file that call the method (keep by default)</param>
        /// <returns>State of <paramref name="logObjectName"/></returns>
        /// <remarks>If the <paramref name="logObjectName"/> doesn't exist, it is added with a default value <see langword="false"/></remarks>
        public bool getToggle(string logObjectName, [CallerFilePath] string callerPath = "")
        {
            return ConfigurationLog.getInstance().getLogObjectToggle(logObjectName, callerPath);
        }

        /// <summary>
        /// Force the writing of the logs into the file
        /// </summary>
        public void Flush()
        {
            mutexFile.WaitOne();
            try
            {
                File.AppendAllText(path, writeTxt, Encoding.GetEncoding("iso-8859-1"));
                File.AppendAllText(path, logTxt, Encoding.GetEncoding("iso-8859-1"));
                //using (StreamWriter file = new StreamWriter(path, true, Encoding.GetEncoding("iso-8859-1")))
                //{
                //    file.Write(logTxt);
                //    file.Close();
                //    file.Dispose();

                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                writeToErrorFile(e);
            }
            writeTxt = "";
            logTxt = "";

            mutexTimer.WaitOne();
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }
            mutexTimer.ReleaseMutex();

            mutexFile.ReleaseMutex();
        }

        private void EnableTimer()
        {
            mutexTimer.WaitOne();
            try
            {
                if (logTxt.Length > 500000)
                {
                    Flush();
                }
                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }
                timer = new System.Timers.Timer()
                {
                    Interval = 1000,
                    AutoReset = false,
                    //Enabled = true,
                };
                timer.Elapsed += (s, e) =>
                {
                    Flush();
                };
                timer.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                writeToErrorFile(e);
            }
            mutexTimer.ReleaseMutex();
        }

        internal string formatMsg(string message, string callerPath, int callerNo, string logName, bool withMillisec)
        {
            callerPath = callerPath.Split('\\').Last();
            DateTime now = DateTime.Now;
            string milliseconds = "";
            if (withMillisec)
            {
                milliseconds += now.Millisecond < 100 ? ".0" : ".";
                milliseconds += now.Millisecond < 10 ? "0" : "";
                milliseconds += now.Millisecond;
            }

            return "[" + now + milliseconds + "] " + callerPath + "(" + logName + "." + callerNo + "): " + message;
        }

        internal static void writeToErrorFile(Exception e)
        {
            mutexError.WaitOne();

            int i = 0;
            do
            {
                string error = "[" + DateTime.Now + "] Error" + i + " " + e.GetType().Name + " => " + e.Message;
                error += "\nStacktrace => " + e.StackTrace + "\n\n";

                internalWriteToErrorFile(error);
                i++;

                e = e.InnerException;
            }
            while (e != null);

            mutexError.ReleaseMutex();
        }

        internal static void writeToErrorFile(string txt)
        {
            mutexError.WaitOne();
            internalWriteToErrorFile(txt + "\n");
            mutexError.ReleaseMutex();
        }

        private static void internalWriteToErrorFile(string txt)
        {
            try
            {
                File.AppendAllText(errorLog, txt, Encoding.GetEncoding("iso-8859-1"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private void doBackup(bool scheduled = false)
        {
            mutexFile.WaitOne();
            try
            {
                DateTime now = DateTime.Now.Date;

                string logPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "Logs";
                string backupPath = Path.Combine(logPath, "Backup");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                else
                {
                    if (!Directory.Exists(backupPath))
                    {
                        Directory.CreateDirectory(backupPath);
                    }

                    // Delete the directory if the date is older than 7 days
                    string[] directories = Directory.GetDirectories(backupPath);
                    if (directories.Length > 0)
                    {
                        foreach (string dir in directories)
                        {
                            string dirDate = Path.GetFileName(dir);

                            DateTime tmpDate;
                            if (DateTime.TryParseExact(dirDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out tmpDate))
                            {
                                if (tmpDate.AddDays(7) < now)
                                {
                                    Directory.Delete(dir, true);
                                }
                            }
                        }
                    }

                    // Created the directory with the date of today
                    string datedPath = Path.Combine(backupPath, now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture));
                    if (!Directory.Exists(datedPath))
                    {
                        backupNumber = 0;
                        Directory.CreateDirectory(datedPath);
                    }


                    directories = Directory.GetDirectories(datedPath);
                    if (directories.Length > 0)
                    {
                        foreach (string dir in directories)
                        {
                            string dirNumber = Path.GetFileNameWithoutExtension(dir);
                            dirNumber = dirNumber.Replace("_scheduled", "");
                            int tmpNumber = 0;
                            if (int.TryParse(dirNumber, out tmpNumber))
                            {
                                if (tmpNumber > backupNumber)
                                {
                                    backupNumber = tmpNumber;
                                }
                            }
                        }
                        backupNumber++;
                    }


                    string[] files = Directory.GetFiles(logPath);
                    if (files.Length > 0)
                    {
                        string autoBackup = scheduled ? "_scheduled" : "";
                        string backupDir = Path.Combine(datedPath, backupNumber.ToString() + autoBackup);

                        if (!Directory.Exists(backupDir))
                        {
                            Directory.CreateDirectory(backupDir);
                        }

                        foreach (string file in files)
                        {
                            string fileNameTmp = Path.GetFileName(file);
                            string backupFile = Path.Combine(backupDir, fileNameTmp);

                            File.Move(file, backupFile);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                writeToErrorFile(e);
            }
            mutexFile.ReleaseMutex();
        }

        private void ResetFile()
        {
            mutexFile.WaitOne();
            string logPath = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "Logs";
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            string path = AppDomain.CurrentDomain.BaseDirectory + Path.DirectorySeparatorChar + "Logs" + Path.DirectorySeparatorChar + filename + ".log";

            try
            {
                FileStream fStrm = File.Create(path);
                fStrm.Close();
                fStrm.Dispose();
                //FileStream ostrm = new FileStream(path, FileMode.Create, FileAccess.Write);
                //StreamWriter writer = new StreamWriter(ostrm);

                //writer.Close();
                //ostrm.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                writeToErrorFile(e);
            }
            mutexFile.ReleaseMutex();
        }
    }
}
