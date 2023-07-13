using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWebSocketReceiver
    {
        void Init();
        string DefineTrigger();
        Type GetBody();

        List<IWebSocketInstance> GetWebSockets();
    }
    public abstract class WebSocketReceiver<T, U> : IWebSocketReceiver where T : IWebSocketReceiver, new() where U : new()
    {
        #region singleton
        private static readonly Dictionary<Type, IWebSocketReceiver> singletons = new();
        public static T? GetInstance()
        {
            Type type = typeof(T);
            if (!singletons.ContainsKey(type))
            {
                singletons[type] = new T();
            }
            if (singletons[type] is T casted)
            {
                return casted;
            }
            return default;
        }
        protected WebSocketReceiver() { }
        #endregion

        private readonly List<IWebSocketInstance> instances = new();

        public virtual void Init()
        {
            string trigger = DefineTrigger();
            DefineWebSockets();
            foreach (IWebSocketInstance instance in instances)
            {
                instance.AddRoute(trigger, async delegate (WebSocketData data)
                {
                    U? item = data.GetData<U>();
                    if (item != null)
                    {
                        await OnMessage(data, item);
                    }
                });
            }
        }

        public abstract string DefineTrigger();
        public Type GetBody()
        {
            return typeof(U);
        }
        public abstract void DefineWebSockets();
        public void SetWebSocket<X>() where X : IWebSocketInstance
        {
            MethodInfo? GetInstance = typeof(X).GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (GetInstance != null)
            {
                IWebSocketInstance? instance = (IWebSocketInstance?)GetInstance.Invoke(null, null);
                if (instance != null)
                {
                    instances.Add(instance);
                }
            }
            
        }
        public List<IWebSocketInstance> GetWebSockets()
        {
            return instances;
        }

        public abstract Task OnMessage(WebSocketData socketData, U message);

    }
}
