using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AventusSharp.WebSocket
{
    public interface IWebSocketReceiver
    {
        void init();
        string defineTrigger();
        Type getBody();

        List<IWebSocketInstance> getWebSockets();
    }
    public abstract class WebSocketReceiver<T, U> : IWebSocketReceiver where T : IWebSocketReceiver, new() where U : new()
    {
        #region singleton
        private static Dictionary<Type, IWebSocketReceiver> singletons = new Dictionary<Type, IWebSocketReceiver>();
        public static T? getInstance()
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
            return default(T);
        }
        protected WebSocketReceiver() { }
        #endregion

        private List<IWebSocketInstance> instances = new List<IWebSocketInstance>();

        public virtual void init()
        {
            string trigger = defineTrigger();
            defineWebSockets();
            foreach (IWebSocketInstance instance in instances)
            {
                instance.addRoute(trigger, async delegate (WebSocketData data)
                {
                    U? item = data.getData<U>();
                    if (item != null)
                    {
                        await onMessage(data, item);
                    }
                });
            }
        }

        public abstract string defineTrigger();
        public Type getBody()
        {
            return typeof(U);
        }
        public abstract void defineWebSockets();
        public void setWebSocket<X>() where X : IWebSocketInstance
        {
            MethodInfo? getInstance = typeof(X).GetMethod("getInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (getInstance != null)
            {
                IWebSocketInstance? instance = (IWebSocketInstance?)getInstance.Invoke(null, null);
                if (instance != null)
                {
                    instances.Add(instance);
                }
            }
            
        }
        public List<IWebSocketInstance> getWebSockets()
        {
            return instances;
        }

        public abstract Task onMessage(WebSocketData socketData, U message);






    }
}
