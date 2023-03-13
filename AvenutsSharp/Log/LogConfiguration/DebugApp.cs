using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AventusSharp.Log.LogConfiguration
{
    internal class DebugApp
    {
        public string name;
        public List<DebugFolder> folders;
        public List<DebugObject> objects;

        public DebugApp(string name)
        {
            this.name = name;
            folders = new List<DebugFolder>() { new DebugFolder("") };
            objects = new List<DebugObject>();
        }

        public bool containsFolder(string folderName)
        {
            foreach (DebugFolder folder in folders)
            {
                if (folder.dirPath == folderName)
                {
                    return true;
                }
            }
            return false;
        }

        public DebugFolder getFolder(string folderName)
        {
            if (containsFolder(folderName))
            {
                return folders.First(app => app.dirPath == folderName);
            }
            return null;
        }

        public DebugFile getFile(string filePath, string filename)
        {
            try
            {
                string fullPath = "";
                List<string> paths = filePath.Split(Path.DirectorySeparatorChar).ToList();

                fullPath += paths.First();

                DebugFolder folder = getFolder(fullPath);
                paths.RemoveAt(0);
                /*
                if (folder is null)
                {
                    return null;
                }
                */
                // Navigate throw the folder hierarchy


                foreach (string path in paths)
                {
                    fullPath += Path.DirectorySeparatorChar + path;
                    folder = folder.getFolder(fullPath);

                    /*
                    if (folder is null)
                    {
                        return null;
                    }
                    */
                }

                return folder.getFile(filename);
            }
            catch (Exception e)
            {
                Log.writeToErrorFile(e);
            }
            return null;
        }


    }
}
