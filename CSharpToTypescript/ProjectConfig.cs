using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CSharpToTypescript
{
    internal class ProjectConfig
    {
        public string csProj = @"D:\Aventus\AvenutsSharp\AvenutsSharp\AventusSharp.csproj";
        public string outputPath = @"D:\Aventus\AvenutsSharp\AvenutsSharp\AventusJs\src";
        private string? _basedir;
        public Assembly compiledAssembly;
        public string baseDir
        {
            get
            {
                _basedir ??= Path.GetDirectoryName(csProj) ?? "";
                return _basedir;
            }
        }
        public bool useNamespace = true;


        public ProjectConfig()
        {
        }


        
    }
}
