using AventusSharp.Routes;
using AventusSharp.WebSocket;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpToTypescript.Container
{
    internal class WsEndPointContainer : BaseClassContainer
    {
        public static bool Is(INamedTypeSymbol type, string fileName, out BaseContainer? result)
        {
            result = null;
            if (type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IWsEndPoint>(p)) != null)
            {
                if (Tools.ExportToTypesript(type, ProjectManager.Config.exportWsEndPointByDefault))
                {
                    result = new WsEndPointContainer(type);
                }
                return true;
            }
            return false;
        }

        public static Dictionary<Type, List<WsEndPointContainerInfo>> _events = new Dictionary<Type, List<WsEndPointContainerInfo>>();
        public static Dictionary<Type, List<WsEndPointContainerInfo>> _routers = new Dictionary<Type, List<WsEndPointContainerInfo>>();
        public static List<WsEndPointContainerInfo> _defaultEvents = new List<WsEndPointContainerInfo>();
        public static List<WsEndPointContainerInfo> _defaultRouters = new List<WsEndPointContainerInfo>();


        private string className = "";
        private string path = "";
        private Type realType;
        private bool isMain = false;
        public WsEndPointContainer(INamedTypeSymbol type) : base(type)
        {
            string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (type.IsGenericType)
            {
                fullName += "`" + type.TypeParameters.Length;
            }
            Type? realType = ProjectManager.Config.compiledAssembly.GetType(fullName);
            if (realType == null)
            {
                throw new Exception("something went wrong on ws end point");
            }
            this.realType = realType;
            WsEndPoint? endPoint = (WsEndPoint?)Activator.CreateInstance(realType);
            if (endPoint == null)
            {
                throw new Exception("something went wrong on ws end point");
            }

            path = endPoint.Path;
            isMain = endPoint.Main();
        }


        protected override string WriteAction()
        {
            List<string> result = new List<string>();
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                AddIndent();
            }

            className = GetTypeName(type, 0, true);

            result.Add(GetConstants());

            string documentation = GetDocumentation(type);
            if (documentation.Length > 0)
            {
                result.Add(documentation);
            }
            string endPoint = GetTypeName(typeof(WsEndPoint));
            AddTxtOpen(GetAccessibilityExport(type) + "class " + className + " extends " + endPoint + ".With(endPointInfo) {", result);
            result.Add(GetContent());
            AddTxtClose("}", result);
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                RemoveIndent();
            }

            return string.Join("\r\n", result);
        }


        private string GetConstants()
        {
            List<string> result = new();

            AddTxtOpen("export const " + className + "Routes = [", result);
            if (_routers.ContainsKey(realType))
            {
                foreach (WsEndPointContainerInfo info in _routers[realType])
                {
                    AddTxt("{ type: " + GetTypeName(info.type) + ", path: \"" + info.path + "\" },", result);
                }
            }
            if (isMain)
            {
                foreach (WsEndPointContainerInfo info in _defaultRouters)
                {
                    AddTxt("{ type: " + GetTypeName(info.type) + ", path: \"" + info.path + "\" },", result);
                }
            }
            AddTxtClose("] as const;", result);
            AddTxt("", result);

            AddTxtOpen("export const " + className + "Events = [", result);
            if (_events.ContainsKey(realType))
            {
                foreach (WsEndPointContainerInfo info in _events[realType])
                {
                    string name = GetTypeName(info.type);
                    string path = info.path;
                    if (path.Length > 0)
                    {
                        path += ".";
                    }
                    path += name;
                    AddTxt("{ type: " + name + ", path: \"" + path + "\" },", result);
                }
            }
            if (isMain)
            {
                foreach (WsEndPointContainerInfo info in _defaultEvents)
                {
                    string name = GetTypeName(info.type);
                    string path = info.path;
                    if (path.Length > 0)
                    {
                        path += ".";
                    }
                    path += name;
                    AddTxt("{ type: " + name + ", path: \"" + path + "\" },", result);
                }
            }
            AddTxtClose("] as const;", result);
            AddTxt("", result);

            AddTxtOpen("const endPointInfo = {", result);
            AddTxt("routes: " + className + "Routes,", result);
            AddTxt("events: " + className + "Events", result);
            AddTxtClose("};", result);
            AddTxt("", result);

            return string.Join("\r\n", result);
        }
        private string GetContent()
        {
            List<string> result = new();
            AddTxtOpen("protected get path(): string {", result);
            AddTxt("return \"" + path + "\";", result);
            AddTxtClose("}", result);
            return string.Join("\r\n", result);
        }

        protected override string? CustomReplacer(ISymbol type, string fullname, string? result)
        {
            return applyReplacer(ProjectManager.Config.replacer.wsEndPoint, fullname, result);
        }
    }


    internal class WsEndPointContainerInfo
    {
        public ISymbol type;
        public string path;

        public WsEndPointContainerInfo(ISymbol type, string path = "")
        {
            this.type = type;
            this.path = path;
        }
    }
}
