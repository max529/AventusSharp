using AventusSharp.WebSocket.Attributes;
using AventusSharp.WebSocket.Event;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Path = AventusSharp.WebSocket.Attributes.Path;

namespace CSharpToTypescript.Container
{
    internal class WsEventContainer : BaseContainer
    {
        public static bool Is(INamedTypeSymbol type, string fileName, out BaseContainer? result)
        {
            result = null;
            if (type.AllInterfaces.ToList().Find(p => Tools.IsSameType<IWebSocketEvent>(p)) != null)
            {
                if (Tools.ExportToTypesript(type, ProjectManager.Config.exportWsEventByDefault))
                {
                    result = new WsEventContainer(type, fileName);
                }
                return true;
            }
            return false;
        }


        public Type realType;
        public string typescriptPath = "";
        public string eventPath = "";
        public string fileName = "";
        private bool? listenOnBoot = null;

        public WsEventContainer(INamedTypeSymbol type, string fileName) : base(type)
        {
            this.fileName = fileName;
            string fullName = type.ContainingNamespace.ToString() + "." + type.Name;
            if (type.IsGenericType)
            {
                fullName += "`" + type.TypeParameters.Length;
            }
            Type? realType = ProjectManager.Config.compiledAssembly.GetType(fullName);
            if (realType == null)
            {
                throw new Exception("something went wrong");
            }
            this.realType = realType;
            this.LoadBody();
            this.ParseAttributes();
        }

        private void LoadBody()
        {
            Type type = realType;
            while(type.BaseType != null)
            {
                Type newType = type.BaseType;
                if(newType.IsGenericType && newType.GetGenericTypeDefinition() == typeof(WsEvent<>))
                {
                    Type body = newType.GetGenericArguments()[0];
                    if(body.IsNested)
                    {
                        ITypeSymbol? typeSymbol = ProjectManager.Compilation.GetTypeByMetadataName(body.FullName ?? "");
                        if(typeSymbol is INamedTypeSymbol namedType)
                        {
                            FileToWrite.AddBaseContainer(new NormalClassContainer(namedType), fileName);
                        }
                    }
                }

                type = newType;
            }
        }

        private void ParseAttributes()
        {
            IEnumerable<Attribute> attrs = realType.GetCustomAttributes();
            bool oneEndPoint = false;
            foreach(Attribute attr in attrs)
            {
                if(attr is EndPoint endPointAttr)
                {
                    if(!WsEndPointContainer._events.ContainsKey(endPointAttr.endpoint))
                    {
                        WsEndPointContainer._events[endPointAttr.endpoint] = new();
                    }

                    WsEndPointContainer._events[endPointAttr.endpoint].Add(new WsEndPointContainerInfo(type, endPointAttr.typescriptPath));
                    oneEndPoint = true;
                }
                else if(attr is Path pathAttr)
                {
                    this.eventPath = pathAttr.pattern;
                }
                else if(attr is ListenOnBoot listenOnBootAttr)
                {
                    listenOnBoot = listenOnBootAttr.listen;
                }
            }
            if(this.eventPath == "")
            {
                this.eventPath = realType.FullName ?? "";
            }
            if(!oneEndPoint)
            {
                WsEndPointContainer._defaultEvents.Add(new WsEndPointContainerInfo(type));
            }
        }


        protected override string WriteAction()
        {
            List<string> result = new List<string>();
            if (ProjectManager.Config.useNamespace && Namespace.Length > 0)
            {
                AddIndent();
            }

            string documentation = GetDocumentation(type);
            if (documentation.Length > 0)
            {
                result.Add(documentation);
            }
            AddTxtOpen(GetAccessibilityExport(type) + GetAbstract() + "class " + GetTypeName(type, 0, true) + " " + GetExtension() + "{", result);
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
            if (type.IsAbstract)
            {
                return "abstract ";
            }
            return "";
        }
        private bool isParsingExtension = false;
        private string GetExtension()
        {
            string extend = "AventusSharp.Socket.WsEvent";
            if (type.BaseType != null && type.BaseType.Name != "Object")
            {
                isParsingExtension = true;
                extend = GetTypeName(type.BaseType);
                isParsingExtension = false;
            }


            string txt = "extends " + extend + " ";

            return txt;
        }

        private string GetContent()
        {
            List<string> result = new();

            AddTxt("/**", result);
            AddTxt(" * @inheritdoc", result);
            AddTxt(" */", result);
            AddTxtOpen("protected override get path(): string {", result);
            AddTxt("return `" + eventPath+"`;", result);
            AddTxtClose("}", result);

            if (listenOnBoot != null)
            {
                string trueTxt = listenOnBoot == true ? "true" : "false";
                AddTxt("", result);
                AddTxt("/**", result);
                AddTxt(" * @inheritdoc", result);
                AddTxt(" */", result);
                AddTxtOpen("protected override listenOnBoot(): boolean {", result);
                AddTxt("return " + trueTxt + ";", result);
                AddTxtClose("}", result);
            }

            return string.Join("\r\n", result);
        }

        protected override string? CustomReplacer(ISymbol type, string fullname, string? result)
        {
            return applyReplacer(ProjectManager.Config.replacer.wsEvent, fullname, result);
        }
    }
}
