
using CSharpToTypescript.Container;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Diagnostics;
using System.Reflection;
using Project = Microsoft.CodeAnalysis.Project;

namespace CSharpToTypescript
{
    internal class ProjectManager
    {
        public static string CurrentAssemblyName { get; private set; }
        public static Compilation Compilation { get; private set; }
        public static ProjectConfig Config { get; private set; }
        public static bool CompilingAventusSharp
        {
            get
            {
                return Config.compiledAssembly.FullName?.StartsWith("AventusSharp,") == true;
            }
        }

        public Dictionary<string, List<BaseContainer>> files = new Dictionary<string, List<BaseContainer>>();

        public ProjectManager()
        {

        }

        public async Task Init(ProjectConfig config)
        {
            Config = config;
            if (!Build())
            {
                Console.WriteLine("Error : Compilation failed");
                return;
            }
            if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();
            using (var w = MSBuildWorkspace.Create())
            {
                Project proj = await w.OpenProjectAsync(config.csProj);
                Compilation = await proj.GetCompilationAsync() ?? throw new Exception("Can't compile");

                List<INamedTypeSymbol> result = new();
                INamespaceSymbol? rootNamespace = Compilation.GlobalNamespace.GetNamespaceMembers().First(p => p.Name == proj.Name);
                if (rootNamespace != null)
                {
                    CurrentAssemblyName = rootNamespace.Name;
                    LoadNamespace(rootNamespace, result);
                }
                FileToWrite.WriteAll();
            }
        }

        private bool Build()
        {
            // Start the child process.
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "cmd.exe";
            string cmd = "dotnet build " + Config.csProj + " --no-dependencies -v m";
            p.StartInfo.Arguments = "/C " + cmd;
            p.Start();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            List<string> splitted = output.Split("\n").ToList();
            string nbErrors = splitted[splitted.Count - 4];
            if (!nbErrors.Contains("0"))
            {
                Console.WriteLine(output);
                return false;
            }
            string outputPath = "";
            for (int i = splitted.Count - 1; i >= 0; i--)
            {
                if (splitted[i].Contains("->"))
                {
                    outputPath = splitted[i].Split("->")[1].Trim();
                }
            }
            if (outputPath == "")
            {
                Console.WriteLine(output);
                return false;
            }
            List<string> outputSplitted = outputPath.Split(Path.DirectorySeparatorChar).ToList();
            outputSplitted.RemoveAt(outputSplitted.Count - 1);
            Config.outputDir = string.Join(Path.DirectorySeparatorChar, outputSplitted);
            Config.compiledAssembly = Assembly.LoadFrom(outputPath);
            return true;
        }

        private void LoadNamespace(INamespaceSymbol @namespace, List<INamedTypeSymbol> result)
        {
            List<INamedTypeSymbol> resultTemp = @namespace.GetTypeMembers().ToList();
            foreach (INamedTypeSymbol type in resultTemp)
            {
                result.Add(type);
                FileToWrite.RegisterType(type);
            }

            var subNamesapce = @namespace.GetNamespaceMembers();

            foreach (INamespaceSymbol symbol in subNamesapce)
            {
                LoadNamespace(symbol, result);
            }
        }

    }
}
