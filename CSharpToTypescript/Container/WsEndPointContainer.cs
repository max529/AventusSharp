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
        public static List<Type> wroteTypes = new List<Type>();


        private string className = "";
        private string fullClassName = "";
        private string path = "";
        private Type realType;
        private bool isMain = false;
        private bool isParentWsEndPoint = true;
        private bool externalCreate = false;

        public WsEndPointContainer(INamedTypeSymbol type, bool externalCreate = false) : base(type)
        {
            this.externalCreate = externalCreate;
            string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (type.IsGenericType)
            {
                fullName += "`" + type.TypeParameters.Length;
            }
            Type? realType = Tools.GetTypeFromFullName(fullName);
            if (realType == null)
            {
                throw new Exception("something went wrong on ws end point");
            }
            this.realType = realType;
            wroteTypes.Add(realType);
            isParentWsEndPoint = (realType.BaseType == typeof(WsEndPoint) && !externalCreate);
            if (realType.IsAbstract) return;

            WsEndPoint? endPoint = (WsEndPoint?)Activator.CreateInstance(realType);
            if (endPoint == null)
            {
                throw new Exception("something went wrong on ws end point");
            }
            path = endPoint.Path;
            isMain = endPoint.Main();
        }

        public WsEndPointContainer(Type type) : this(Tools.GetNameTypeSymbol(type), true)
        {

        }

        protected override string WriteAction()
        {

            List<string> result = new List<string>();
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                AddIndent();
            }

            fullClassName = GetTypeName(type, 0, true);
            className = fullClassName.Split(".").Last();

            result.Add(GetConstants());

            string documentation = GetDocumentation(type);
            if (documentation.Length > 0)
            {
                result.Add(documentation);
            }
            AddTxtOpen(GetAccessibilityExport(type) + GetAbstract() + "class " + className + " extends " + className + "Type {", result);
            result.Add(GetContent());
            AddTxtClose("}", result);
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                RemoveIndent();
            }

            return string.Join("\r\n", result);
        }

        private string GetAbstract()
        {
            if (!isInterface && type.IsAbstract)
            {
                return "abstract ";
            }
            return "";
        }

        private string GetConstants()
        {
            List<string> result = new();

            AddTxtOpen("export const " + className + "Routes: [", result);
            List<string> routes = new List<string>();
            if (_routers.ContainsKey(realType))
            {
                foreach (WsEndPointContainerInfo info in _routers[realType])
                {
                    AddTxt("AventusSharp.WebSocket.RouteType<typeof " + GetTypeName(info.type) + ", \"" + info.path + "\">,", result);
                    routes.Add("{ type: " + GetTypeName(info.type) + ", path: \"" + info.path + "\" },");
                }
            }
            if (isMain)
            {
                foreach (WsEndPointContainerInfo info in _defaultRouters)
                {
                    AddTxt("AventusSharp.WebSocket.RouteType<typeof " + GetTypeName(info.type) + ", \"" + info.path + "\">,", result);
                    routes.Add("{ type: " + GetTypeName(info.type) + ", path: \"" + info.path + "\" },");
                }
            }
            AddTxt("] = [", result);
            foreach (string route in routes)
            {
                AddTxt(route, result);
            }
            AddTxtClose("] as const;", result);
            AddTxt("", result);

            List<string> events = new List<string>();
            AddTxtOpen("export const " + className + "Events: [", result);
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
                    AddTxt("AventusSharp.WebSocket.EventType<typeof " + name + ", \"" + path + "\">,", result);
                    events.Add("{ type: " + name + ", path: \"" + path + "\" },");
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
                    AddTxt("AventusSharp.WebSocket.EventType<typeof " + name + ", \"" + path + "\">,", result);
                    events.Add("{ type: " + name + ", path: \"" + path + "\" },");
                }
            }
            AddTxt("] = [", result);
            foreach (string _event in events)
            {
                AddTxt(_event, result);
            }
            AddTxtClose("] as const;", result);
            AddTxt("", result);

            string parentName = "";
            if (!isParentWsEndPoint && realType.BaseType != null)
            {
                if (externalCreate)
                {
                    parentName = fullClassName;
                }
                else
                {
                    parentName = GetTypeName(realType.BaseType);
                }
            }

            AddTxtOpen("export const " + className + "Type: AventusSharp.WebSocket.EndPointType<{", result);
            AddTxt("routes: typeof " + className + "Routes,", result);
            AddTxt("events: typeof " + className + "Events", result);
            if (isParentWsEndPoint)
            {
                AddTxt("}> = AventusSharp.WebSocket.EndPoint.WithRoute({", result);
            }
            else
            {
                AddTxt("}, typeof " + parentName + "> = AventusSharp.WebSocket.EndPoint.WithRoute({", result);
            }
            AddTxt("routes: " + className + "Routes,", result);
            AddTxt("events: " + className + "Events", result);
            if (isParentWsEndPoint)
            {
                AddTxtClose("});", result);
            }
            else
            {
                AddTxtClose("}, "+parentName+");", result);
            }
            AddTxt("", result);

            return string.Join("\r\n", result);
        }
        private string GetContent()
        {
            List<string> result = new();
            if (!realType.IsAbstract)
            {
                AddTxt("", result);
                AddTxt("/**", result);
                AddTxt(" * Create a singleton", result);
                AddTxt(" */", result);
                if (isParentWsEndPoint)
                {
                    AddTxtOpen("public static getInstance(): MainEndPoint {", result);
                }
                else
                {
                    AddTxtOpen("public static override getInstance(): MainEndPoint {", result);
                }
                AddTxt("return Aventus.Instance.get(MainEndPoint);", result);
                AddTxtClose("}", result);
                AddTxt("", result);
                if (isParentWsEndPoint)
                {
                    AddTxtOpen("protected get path(): string {", result);
                }
                else
                {
                    AddTxtOpen("protected override get path(): string {", result);
                }
                AddTxt("return \"" + path + "\";", result);
                AddTxtClose("}", result);
            }
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
