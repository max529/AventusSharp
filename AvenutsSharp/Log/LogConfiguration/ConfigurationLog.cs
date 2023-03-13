using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace AventusSharp.Log.LogConfiguration
{
    internal class ConfigurationLog
    {
        private static readonly Mutex mutex = new Mutex();
        private static ConfigurationLog _instance;

        private readonly Mutex nonStaticMutex;
        private readonly FileSystemWatcher watcher;

        private readonly char separator = Path.DirectorySeparatorChar;

        private static readonly List<int> lineWithError = new List<int>();
        private readonly JsonSerializerSettings serializer = new JsonSerializerSettings();

        private readonly string logConfigPath;
        private readonly string configFile;

        private readonly List<string> appName = new List<string>() { "Service", "WebApp", "Library", "Others" };
        private readonly DebugRoot objRoot;

        private readonly Dictionary<string, List<DebugLine>> allLine;

        private readonly string servicePath = "D:\\404\\5_Prog_SVN\\2_Services\\";
        private readonly string libraryPath = "D:\\404\\5_Prog_SVN\\5_Templates\\Libraries\\";
        private readonly string webappPath = "D:\\404\\5_Prog_SVN\\1_WebApps\\";

        private ConfigurationLog()
        {
            serializer.Formatting = Formatting.Indented;
            serializer.Error = JsonErrorHandler;

            nonStaticMutex = new Mutex();
            logConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.Combine("Logs", "LogConfig"));
            allLine = new Dictionary<string, List<DebugLine>>();

            if (!Directory.Exists(logConfigPath))
            {
                Directory.CreateDirectory(logConfigPath);
            }

            configFile = Path.Combine(logConfigPath, "LogConfiguration.json");
            nonStaticMutex.WaitOne();
            objRoot = new DebugRoot();
            try
            {
                if (File.Exists(configFile))
                {
                    string configTmp = File.ReadAllText(configFile);
                    if (string.IsNullOrEmpty(configTmp) || configTmp.Equals("null"))
                    {
                        objRoot = new DebugRoot();
                    }
                    else
                    {
                        objRoot = JsonConvert.DeserializeObject<DebugRoot>(configTmp, serializer);

                        List<string> configLines = File.ReadAllLines(configFile).ToList();

                        while (objRoot is null)
                        {
                            lineWithError.Reverse();
                            foreach (int lineToDelete in lineWithError)
                            {
                                configLines.RemoveAt(lineToDelete - 1);
                            }

                            lineWithError.Clear();
                            configTmp = string.Join("\n", configLines);
                            objRoot = JsonConvert.DeserializeObject<DebugRoot>(configTmp, serializer);
                        }
                    }
                }
                string configJson = JsonConvert.SerializeObject(objRoot, serializer);
                File.WriteAllText(configFile, configJson);
            }
            catch (Exception e)
            {
                Log.writeToErrorFile("Error for serialize/deserialize DebugRoot");
                Log.writeToErrorFile(e);
            }

            //setAllApps(objRoot);


            nonStaticMutex.ReleaseMutex();
            // Create a new FileSystemWatcher and set its properties.
            watcher = new FileSystemWatcher
            {
                Path = logConfigPath,

                // Watch for changes in LastWrite times, and
                // the renaming of files or directories.
                NotifyFilter = NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName,

                // Only watch LogConfiguration.json file.
                Filter = "LogConfiguration.json"
            };

            // Add event handlers.
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnRenamed;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        private void JsonErrorHandler(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
        {
            if (e.CurrentObject == e.ErrorContext.OriginalObject)
            {
                int line;

                // TODO Check all the lines that causes problem and delete them
                string errorMessage = e.ErrorContext.Error.Message;

                Match mLine = Regex.Match(errorMessage, "line [0-9]+");
                if (mLine.Success)
                {
                    string res = mLine.Value;
                    line = int.Parse(res.Replace("line ", ""));
                    if (!lineWithError.Contains(line))
                    {
                        lineWithError.Add(line);
                    }
                }
                e.ErrorContext.Handled = true;
            }
        }

        /// <summary>
        /// Get the singleton of the ConfigurationLog
        /// </summary>
        /// <returns></returns>
        public static ConfigurationLog getInstance()
        {
            mutex.WaitOne();
            if (_instance == null)
            {
                _instance = new ConfigurationLog();
            }
            mutex.ReleaseMutex();
            return _instance;
        }

        // Define the event handlers.
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            nonStaticMutex.WaitOne();
            // Specify what is done when a file is changed, created, or deleted.
            try
            {
                Thread.Sleep(500);
                //Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
                // Save the new config into configuration
                string configJson = File.ReadAllText(e.FullPath);

                DebugRoot objRootTmp = JsonConvert.DeserializeObject<DebugRoot>(configJson, serializer);

                List<string> configLines = configJson.Split('\n').ToList();
                bool hasError = false;
                while (objRootTmp is null)
                {
                    hasError = true;
                    lineWithError.Reverse();
                    foreach (int lineToDelete in lineWithError)
                    {
                        configLines.RemoveAt(lineToDelete - 1);
                    }

                    lineWithError.Clear();
                    configJson = string.Join("\n", configLines);
                    objRootTmp = JsonConvert.DeserializeObject<DebugRoot>(configJson, serializer);
                }

                if (setObjRoot(objRootTmp) || hasError)
                {
                    setConfigFromObjRoot();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            nonStaticMutex.ReleaseMutex();
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            nonStaticMutex.WaitOne();
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
            nonStaticMutex.ReleaseMutex();
        }

        private void setConfigFromObjRoot()
        {
            try
            {
                watcher.EnableRaisingEvents = false;
            }
            catch (Exception e)
            {
                Log.writeToErrorFile("Error for enableRaisingEvents false");
                Log.writeToErrorFile(e);
            }
            try
            {
                string configJson = JsonConvert.SerializeObject(objRoot, serializer);
                File.WriteAllText(configFile, configJson);
            }
            catch (Exception e)
            {
                Log.writeToErrorFile(e);
            }
            try
            {
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception e)
            {
                Log.writeToErrorFile("Error for enableRaisingEvents true");
                Log.writeToErrorFile(e);
            }
        }

        private bool setObjRoot(DebugRoot debugRoot)
        {
            bool hasModification = false;
            foreach (string app in appName)
            {
                List<DebugApp> list = debugRoot.getList(app);

                if (list.Count > 0)
                {
                    objRoot.setList(app, list);
                }
                else
                {
                    if (objRoot.getList(app).Count > 0)
                    {
                        hasModification = true;
                        string callerPath = getCallerPath(app);

                        List<string> keysToRemove = allLine.Keys.Where(key => key.StartsWith(callerPath)).ToList();

                        foreach (string key in keysToRemove)
                        {
                            allLine.Remove(key);
                        }
                        objRoot.setList(app, new List<DebugApp>());
                    }
                }
            }
            return hasModification;
        }

        private string getCallerPath(string applicationPath)
        {
            string callerPath;
            if (applicationPath.StartsWith("Service"))
            {
                callerPath = servicePath;
            }
            else if (applicationPath.StartsWith("Library"))
            {
                callerPath = libraryPath;

            }
            else if (applicationPath.StartsWith("WebApp"))
            {
                callerPath = webappPath;
            }
            else
            {
                callerPath = "";
            }
            return callerPath;
        }

        private string getAppPath(string callerPath)
        {
            string applicationPath;
            if (callerPath.StartsWith(servicePath))
            {
                applicationPath = servicePath;
            }
            else if (callerPath.StartsWith(libraryPath))
            {
                applicationPath = libraryPath;

            }
            else if (callerPath.StartsWith(webappPath))
            {
                applicationPath = webappPath;
            }
            else
            {
                List<string> temp = callerPath.Split('\\').ToList();
                temp.RemoveAt(temp.Count - 1);

                applicationPath = string.Join("\\", temp);
            }
            return applicationPath;
        }

        private List<DebugApp> getDebugApps(string callerPath)
        {
            List<DebugApp> debugApps;
            if (callerPath.StartsWith(servicePath))
            {
                //debugApps = allApps["Service"];
                debugApps = objRoot.Service;
            }
            else if (callerPath.StartsWith(libraryPath))
            {
                //debugApps = allApps["Library"];
                debugApps = objRoot.Library;
            }
            else if (callerPath.StartsWith(webappPath))
            {
                //debugApps = allApps["WebApp"];
                debugApps = objRoot.WebApp;
            }
            else
            {
                //debugApps = allApps["Others"];
                debugApps = objRoot.Others;
            }

            return debugApps;
        }

        internal bool getLogToggle(string callerPath, DebugLine debugLine)
        {
            nonStaticMutex.WaitOne();
            bool isToggle = false;
            try
            {
                string applicationPath = getAppPath(callerPath);
                List<DebugApp> tmpAppDebugs = getDebugApps(applicationPath);

                // Delete the begging of the path
                string tmpFile = callerPath.Substring(applicationPath.Length);
                List<string> tmpPath = tmpFile.Split('\\').ToList();

                // Get the name of the application (Library, Service or WebApp)
                string applicationName = tmpPath.First();

                // Remove all the applicationName, so the remaining string are the exact path of the file
                tmpPath.RemoveAt(0);
                if (tmpPath.FirstOrDefault().Equals(applicationName))
                {
                    tmpPath.RemoveAt(0);
                }
                //tmpPath.RemoveAll(s => s.Equals(applicationName));
                tmpFile = string.Join("" + separator, tmpPath);

                string dirName = Path.GetDirectoryName(tmpFile);
                string fileName = Path.GetFileName(tmpFile);

                DebugApp debugApp = tmpAppDebugs.FirstOrDefault(app => app.name == applicationName);
                if (debugApp != null)
                {
                    DebugFile file = debugApp.getFile(dirName, fileName);
                    if (file is null)
                    {
                        List<string> keysToRemove = allLine.Keys.Where(key => key.StartsWith(callerPath)).ToList();

                        foreach (string key in keysToRemove)
                        {
                            allLine.Remove(key);
                        }

                        nonStaticMutex.ReleaseMutex();
                        setLogToggle(callerPath, debugLine, true);
                        return false;
                    }
                    else
                    {
                        int index = file.lines.FindIndex(dl => dl.outFile == debugLine.outFile && dl.name == debugLine.name);
                        if (index >= 0)
                        {
                            debugLine = file.lines[index];
                        }
                        else
                        {
                            nonStaticMutex.ReleaseMutex();
                            setLogToggle(callerPath, debugLine, true);
                            return false;
                        }

                        //DebugLine debugLine = file.lines.FirstOrDefault(l => l.outFile == outfile && l.name == logName);
                        if (debugLine is null)
                        {
                            nonStaticMutex.ReleaseMutex();
                            setLogToggle(callerPath, debugLine, true);
                            return false;
                        }
                        else
                        {
                            isToggle = debugLine.active;
                        }
                    }
                }
                else
                {
                    nonStaticMutex.ReleaseMutex();
                    setLogToggle(callerPath, debugLine);
                    return debugLine.active;
                }
            }
            catch (Exception e)
            {
                Log.writeToErrorFile(e);
            }
            nonStaticMutex.ReleaseMutex();
            return isToggle;

        }

        /// <summary>
        /// Add the log into DebugRoot and allLine if not exist
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="debugLine"></param>
        /// <param name="forceSet"></param>
        internal void setLogToggle(string callerPath, DebugLine debugLine, bool forceSet = false)
        {
            nonStaticMutex.WaitOne();
            try
            {
                if (allLine.ContainsKey(callerPath))
                {
                    if (allLine[callerPath].Contains(debugLine))
                    {

                        if (!forceSet)
                        {
                            nonStaticMutex.ReleaseMutex();
                            return;
                        }
                        else
                        {
                            int index = allLine[callerPath].FindIndex(df => df == debugLine);
                            if (index >= 0)
                            {
                                allLine[callerPath][index] = debugLine;
                            }
                            else
                            {
                                allLine[callerPath].Add(debugLine);
                            }
                        }
                    }
                    else
                    {
                        allLine[callerPath].Add(debugLine);
                    }
                }
                else
                {
                    allLine[callerPath] = new List<DebugLine>() { debugLine };
                }


                string applicationPath = getAppPath(callerPath);
                List<DebugApp> tmpAppDebugs = getDebugApps(applicationPath);

                // Delete the begging of the path
                string tmpFile = callerPath.Substring(applicationPath.Length);
                List<string> tmpPath = tmpFile.Split('\\').ToList();
                // Get the name of the application (Library, Service or WebApp)
                string applicationName = tmpPath.First();

                // Add the log for the application if not existed
                if (!tmpAppDebugs.Exists(app => app.name == applicationName))
                {
                    tmpAppDebugs.Add(new DebugApp(applicationName));
                }

                // Remove all the applicationName, so the remaining string are the exact path of the file
                //tmpPath.RemoveAll(s => s.Equals(applicationName));
                tmpPath.RemoveAt(0);
                if (tmpPath.FirstOrDefault().Equals(applicationName))
                {
                    tmpPath.RemoveAt(0);
                }

                tmpFile = string.Join("" + separator, tmpPath);
                string dirName = Path.GetDirectoryName(tmpFile);
                string fileName = Path.GetFileName(tmpFile);

                //DebugLine debugLine = new DebugLine(logName, log, noLine, outfile);
                DebugFile file = new DebugFile(fileName);
                file.lines.Add(debugLine);


                DebugApp debugApplication = tmpAppDebugs.First(app => app.name == applicationName);
                DebugFolder tmpDebugFolder = debugApplication.getFolder("");
                DebugFolder previousDebugFolder = tmpDebugFolder;

                string fullPath = dirName;
                bool first = true;
                foreach (string dir in dirName.Split(separator).Reverse())
                {

                    if (first)
                    {
                        first = false;
                        tmpDebugFolder = new DebugFolder(fullPath, new List<DebugFile>() { file });
                    }
                    else
                    {
                        tmpDebugFolder = new DebugFolder(fullPath);
                        tmpDebugFolder.folders.Add(previousDebugFolder);
                    }
                    int index = fullPath.LastIndexOf(dir);
                    if (index > 1)
                    {
                        fullPath = fullPath.Substring(0, index - 1);
                    }
                    else
                    {

                    }
                    previousDebugFolder = new DebugFolder(tmpDebugFolder.dirPath, tmpDebugFolder.files, tmpDebugFolder.folders);
                }

                if (!debugApplication.containsFolder(tmpDebugFolder.dirPath))
                {
                    debugApplication.folders.Add(tmpDebugFolder);
                    setConfigFromObjRoot();
                }
                else
                {
                    DebugFolder tmp = debugApplication.getFolder(tmpDebugFolder.dirPath);
                    bool merged = tmp.mergeFolder(tmpDebugFolder);

                    if (merged)
                    {
                        setConfigFromObjRoot();
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(e.ToString());
                Log.writeToErrorFile(e);
            }

            nonStaticMutex.ReleaseMutex();
            return;
        }

        internal bool getLogObjectToggle(string logName, string callerPath)
        {
            nonStaticMutex.WaitOne();

            bool isToggle = false;
            string applicationPath = getAppPath(callerPath);
            List<DebugApp> tmpAppDebugs = getDebugApps(applicationPath);

            // Delete the begging of the path
            string tmpFile = callerPath.Substring(applicationPath.Length);
            List<string> tmpPath = tmpFile.Split('\\').ToList();

            // Get the name of the application (Library, Service or WebApp)
            string applicationName = tmpPath.First();

            // Add the log for the application if not existed
            if (!tmpAppDebugs.Exists(app => app.name == applicationName))
            {
                tmpAppDebugs.Add(new DebugApp(applicationName));
            }


            DebugApp debugApp = tmpAppDebugs.FirstOrDefault(app => app.name == applicationName);
            if (debugApp != null)
            {
                if (debugApp.objects.Exists(o => o.name == logName))
                {
                    DebugObject debugObj = debugApp.objects.First(o => o.name == logName);
                    isToggle = debugObj.active;
                }
                else
                {
                    debugApp.objects.Add(new DebugObject(logName, false));
                    setConfigFromObjRoot();
                }
            }
            else
            {
                LogError.getInstance("error_debug").WriteLine("Shouldn't enter here..... " + applicationName + " " + logName + " " + callerPath);
            }
            nonStaticMutex.ReleaseMutex();

            return isToggle;
        }

        internal void setLogObjectToggle(string logName, bool log, string callerPath)
        {
            nonStaticMutex.WaitOne();

            string applicationPath = getAppPath(callerPath);
            List<DebugApp> tmpAppDebugs = getDebugApps(applicationPath);

            // Delete the begging of the path
            string tmpFile = callerPath.Substring(applicationPath.Length);

            List<string> tmpPath = tmpFile.Split('\\').ToList();

            // Get the name of the application (Library, Service or WebApp)
            string applicationName = tmpPath.First();

            // Add the log for the application if not existed
            if (!tmpAppDebugs.Exists(app => app.name == applicationName))
            {
                tmpAppDebugs.Add(new DebugApp(applicationName));
            }


            DebugApp debugApp = tmpAppDebugs.FirstOrDefault(app => app.name == applicationName);
            DebugObject debugObject = new DebugObject(logName, log);

            if (debugApp != null)
            {
                if (!debugApp.objects.Exists(o => o.name == logName))
                {
                    debugApp.objects.Add(debugObject);
                    setConfigFromObjRoot();
                }
            }
            else
            {
                LogError.getInstance("error_debug").WriteLine("Shouldn't enter here..... " + applicationName + " " + logName + " " + callerPath);
            }
            nonStaticMutex.ReleaseMutex();
        }
    }
}
