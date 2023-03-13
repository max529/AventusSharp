using System.Collections.Generic;
using System.Linq;

namespace AventusSharp.Log.LogConfiguration
{
    internal class DebugFolder
    {
        public string dirPath;
        //public string fullDirPath;
        public List<DebugFolder> folders;
        public List<DebugFile> files;

        public DebugFolder(string dirPath, List<DebugFile> files = null, List<DebugFolder> folders = null)
        {
            //this.fullDirPath = fullDirPath;
            this.dirPath = dirPath;
            this.files = files ?? new List<DebugFile>();
            this.folders = folders ?? new List<DebugFolder>();

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

        public bool containsFile(string fileName)
        {
            foreach (DebugFile file in files)
            {
                if (file.filePath == fileName)
                {
                    return true;
                }
            }
            return false;
        }

        public DebugFile getFile(string fileName)
        {
            if (containsFile(fileName))
            {
                return files.First(f => f.filePath == fileName);
            }
            return null;
        }

        public bool mergeFolders(List<DebugFolder> folders)
        {
            bool merged = false;
            foreach (DebugFolder debugFolder in folders)
            {
                if (mergeFolder(debugFolder))
                {
                    merged = true;
                }
            }
            return merged;
        }

        public bool mergeFolder(DebugFolder debugFolder)
        {
            bool merged = false;
            if (dirPath == debugFolder.dirPath)
            {
                if (mergeFolders(debugFolder.folders))
                {
                    merged = true;
                }
                if (mergeFiles(debugFolder.files))
                {
                    merged = true;
                }
            }
            else
            {
                int index = folders.FindIndex(df => df == debugFolder);
                if (index < 0)
                {
                    folders.Add(debugFolder);
                    merged = true;
                }
                else
                {
                    merged = folders[index].mergeFolder(debugFolder);
                }
            }
            return merged;
        }

        public bool mergeFiles(List<DebugFile> files)
        {
            bool merged = false;

            foreach (DebugFile debugFile in files)
            {
                if (mergeFile(debugFile))
                {
                    merged = true;
                }
            }
            return merged;
        }

        public bool mergeFile(DebugFile debugFile)
        {
            bool merged = false;

            int index = files.FindIndex(f => f == debugFile);
            if (index >= 0)
            {
                merged = files[index].merge(debugFile);
            }
            else
            {
                files.Add(debugFile);
                merged = true;
            }
            return merged;
        }




        public override bool Equals(object obj)
        {
            if (obj is DebugFolder folder)
            {
                return this == folder;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(DebugFolder x, DebugFolder y)
        {
            if (x is null || y is null)
            {
                return x is null && y is null;
            }
            return x.dirPath == y.dirPath;
        }

        public static bool operator !=(DebugFolder x, DebugFolder y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

