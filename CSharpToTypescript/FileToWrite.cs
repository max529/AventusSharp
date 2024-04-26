﻿using CSharpToTypescript.Container;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToTypescript
{
    internal class FileToWrite
    {

        private static Dictionary<string, FileToWrite> allFiles = new();
        private static Dictionary<ISymbol, string> customFileNames = [];
        public static string? GetFileName(ISymbol symbol)
        {
            if(customFileNames.ContainsKey(symbol)) return customFileNames[symbol];

            string? fileName = symbol.Locations[0].SourceTree?.FilePath;
            if (fileName == null)
            {
                return fileName;
            }
            fileName = fileName.Replace(ProjectManager.Config.baseDir + Path.DirectorySeparatorChar, "").Replace(".cs", "");
            fileName = Path.Combine(ProjectManager.Config.outputPath, fileName);
            return fileName;
        }
        public static BaseContainer? GetContainer(ISymbol? symbol)
        {
            if (symbol == null) return null;

            foreach (FileToWrite file in allFiles.Values)
            {
                foreach (BaseContainer container in file.types)
                {
                    if (Tools.GetFullName(symbol) == Tools.GetFullName(container.type))
                    {
                        return container;
                    }
                }
            }
            return null;
        }
        public static void RegisterType(INamedTypeSymbol type)
        {
            string? fileName = GetFileName(type);
            if (fileName == null)
            {
                return;
            }
            BaseContainer? result = null;
            if (EnumContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (StorableContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (HttpRouterContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (WithErrorContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (GenericErrorContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (WsEndPointContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (WsEventContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (WsRouterContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
            else if (NormalClassContainer.Is(type, fileName, out result))
            {
                AddBaseContainer(result, fileName);
            }
        }
        public static void AddBaseContainer(BaseContainer? result, string fileName)
        {
            if (result != null)
            {
                if (!allFiles.ContainsKey(fileName))
                {
                    allFiles.Add(fileName, new FileToWrite(fileName));
                }
                allFiles[fileName].AddContainer(result);
            }
        }

        public static void WriteAll()
        {
            AddOthersFilesBeforeWrite();
            foreach (KeyValuePair<string, FileToWrite> file in allFiles)
            {
                foreach (BaseContainer container in file.Value.types)
                {
                    container.Write();
                }
            }
            foreach (KeyValuePair<string, FileToWrite> file in allFiles)
            {
                file.Value.Resolve();
                file.Value.Write();
            }
            AddOthersFiles();
        }


        private string path;
        private readonly List<BaseContainer> types = new List<BaseContainer>();
        private readonly Dictionary<string, List<BaseContainer>> namespaces = new Dictionary<string, List<BaseContainer>>();
        private readonly Dictionary<string, List<string>> importByPath = new Dictionary<string, List<string>>();

        private string? _extension;
        public string Extension
        {
            get
            {
                if (_extension == null)
                {
                    _extension = GetExtension();
                }
                return _extension;
            }
        }
        private FileToWrite(string path)
        {
            this.path = path;
        }

        public void AddContainer(BaseContainer container)
        {
            if (container.CanBeAdded && !types.Contains(container))
            {
                types.Add(container);

                string @namespace = "";
                if (ProjectManager.Config.useNamespace)
                {
                    @namespace = container.Namespace;
                }

                if (!namespaces.ContainsKey(@namespace))
                {
                    namespaces[@namespace] = new();
                }
                namespaces[@namespace].Add(container);
            }

        }

        private string GetExtension()
        {
            string result = ".lib.avt";

            foreach (var type in types)
            {
                if (type is StorableContainer)
                {
                    if (result == ".lib.avt")
                    {
                        result = ".data.avt";
                    }
                    else if (result == ".data.avt")
                    {
                        continue;
                    }
                    else
                    {
                        result = ".lib.avt";
                    }
                }
                else if (type is EnumContainer)
                {
                    continue;
                }
            }
            return result;
        }

        public void Resolve()
        {
            foreach (BaseContainer container in types)
            {
                string? currentFileName = GetFileName(container.type);
                if (currentFileName == null)
                {
                    continue;
                }
                foreach (ISymbol symbol in container.unresolved)
                {
                    string? fileNameToResolve = GetFileName(symbol);
                    if (fileNameToResolve != null && allFiles.ContainsKey(fileNameToResolve))
                    {
                        string importFile = fileNameToResolve + allFiles[fileNameToResolve].Extension;
                        string relativePath = Tools.GetRelativePath(currentFileName, importFile);
                        if (!importByPath.ContainsKey(relativePath))
                        {
                            importByPath.Add(relativePath, new());
                        }
                        string name = symbol.Name;
                        if (symbol is ITypeSymbol typeSymbol && typeSymbol.TypeKind == TypeKind.Interface)
                        {
                            name = "type " + name;
                        }
                        if (!importByPath[relativePath].Contains(name))
                        {
                            importByPath[relativePath].Add(name);
                        }
                    }
                }
                foreach (KeyValuePair<string, List<string>> customImport in container.importedFiles)
                {
                    string relativePath = Tools.GetRelativePath(currentFileName, customImport.Key);
                    if (!importByPath.ContainsKey(relativePath))
                    {
                        importByPath.Add(relativePath, new());
                    }
                    foreach (string importName in customImport.Value)
                    {
                        if (!importByPath[relativePath].Contains(importName))
                        {
                            importByPath[relativePath].Add(importName);
                        }
                    }
                }
            }
        }

        public void Write()
        {
            List<string> txt = new();
            foreach (KeyValuePair<string, List<string>> import in importByPath)
            {
                txt.Add("import { " + string.Join(", ", import.Value) + " } from '" + import.Key + "';");
            }
            if (importByPath.Count > 0)
            {
                txt.Add("");
            }
            foreach (KeyValuePair<string, List<BaseContainer>> @namespace in namespaces)
            {
                txt.Add("namespace " + @namespace.Key + " {");
                foreach (BaseContainer container in @namespace.Value)
                {
                    txt.Add("");
                    txt.Add(container.Content);
                }
                txt.Add("");
                txt.Add("}");

            }

            string? dirName = Path.GetDirectoryName(path);
            if (dirName != null)
            {
                Directory.CreateDirectory(dirName);
            }

            File.WriteAllText(path + Extension, string.Join("\r\n", txt));
        }

        public static void AddOthersFiles()
        {
            AddRouterFile();
        }

        private static void AddRouterFile()
        {
            FileWriter fileWriter = new FileWriter();

            ProjectConfigHttpRouter routerConfig = ProjectManager.Config.httpRouter;
            if (!routerConfig.createRouter)
            {
                return;
            }
            Dictionary<string, List<string>> imports = new();
            string path = Path.Combine(ProjectManager.Config.outputPath, routerConfig.routerName + ".lib.avt");
            if (!string.IsNullOrWhiteSpace(routerConfig._namespace))
            {
                fileWriter.AddTxtOpen("namespace " + routerConfig._namespace + " {");
            }
            fileWriter.AddTxtOpen("export const " + routerConfig.variableRoutesName + " : [");
            List<string> routes = new List<string>();
            int nbRoute = 0;
            foreach (KeyValuePair<string, FileToWrite> file in allFiles)
            {
                foreach (BaseContainer container in file.Value.types)
                {
                    if (container is HttpRouterContainer http)
                    {
                        if (!http.realType.IsAbstract)
                        {
                            nbRoute++;
                            routes.Add("{ type: " + http.type.Name + ", path: \"" + http.routePath + "\" },");
                            fileWriter.AddTxt($"Aventus.HttpRouteType<typeof {http.type.Name}, \"{http.routePath}\">,");
                            string importFileName = GetFileName(http.type) ?? "";
                            string relativePath = Tools.GetRelativePath(path, importFileName);
                            string importFile = relativePath + allFiles[importFileName].Extension;
                            if (!imports.ContainsKey(importFile))
                            {
                                imports.Add(importFile, new List<string>());
                            }
                            imports[importFile].Add(http.type.Name);
                        }
                    }
                }
            }
            fileWriter.AddTxt("] = [");
            foreach(string route in routes)
            {
                fileWriter.AddTxt(route);
            }
            fileWriter.AddTxtClose("] as const;");

            string host = routerConfig.host ?? "location.protocol + \"//\" + location.host";
            host += " + \"" + routerConfig.uri + "\"";
            fileWriter.AddTxt("");
            fileWriter.AddTxt($"export const {routerConfig.routerName}Type: Aventus.HttpRouterType<typeof {routerConfig.variableRoutesName}> = Aventus.HttpRouter.WithRoute({routerConfig.variableRoutesName})");
            fileWriter.AddTxtOpen($"export class {routerConfig.routerName} extends {routerConfig.routerName}Type {{");
            fileWriter.AddTxtOpen("protected override defineOptions(options: Aventus.HttpRouterOptions): Aventus.HttpRouterOptions {");
            fileWriter.AddTxt($"options.url = {host};");
            fileWriter.AddTxt("return options;");
            fileWriter.AddTxtClose("}");
            fileWriter.AddTxtClose("}");

            if (!string.IsNullOrWhiteSpace(routerConfig._namespace))
            {
                fileWriter.AddTxtClose("}");
            }
            string importTxt = "";
            foreach (var pair in imports)
            {
                importTxt += "import { " + string.Join(", ", pair.Value) + " } from \"" + pair.Key + "\";\r\n";
            }
            importTxt += "\r\n";
            if (nbRoute > 0)
            {
                File.WriteAllText(path, importTxt + fileWriter.GetContent());
            }

        }

        private static void AddOthersFilesBeforeWrite()
        {
            AddMissingWsEndPoint();
        }

        private static void AddMissingWsEndPoint()
        {
            foreach(Type key in WsEndPointContainer._events.Keys)
            {
                if(!WsEndPointContainer.wroteTypes.Contains(key))
                {
                    string fileName = Path.Combine(ProjectManager.Config.outputPath, "Websocket", key.Name);
                    WsEndPointContainer endPoint = new WsEndPointContainer(key);
                    customFileNames[endPoint.type] = fileName;
                    AddBaseContainer(endPoint, fileName);
                }
            }
            foreach (Type key in WsEndPointContainer._routers.Keys)
            {
                if (!WsEndPointContainer.wroteTypes.Contains(key))
                {
                    string fileName = Path.Combine(ProjectManager.Config.outputPath, "Websocket", key.Name);
                    WsEndPointContainer endPoint = new WsEndPointContainer(key);
                    customFileNames[endPoint.type] = fileName;
                    AddBaseContainer(endPoint, fileName);
                }
            }
        }
    }
}
