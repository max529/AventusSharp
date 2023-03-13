using AventusSharp.Data.Storage.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AventusSharp.Data.Manager
{
    public static class GenericDM
    {
        private static Dictionary<Type, IGenericDM> dico = new Dictionary<Type, IGenericDM>();

        public static IGenericDM Get<U>() where U : IStorable
        {
            if (dico.ContainsKey(typeof(U)))
            {
                return dico[typeof(U)];
            }
            throw new Exception("Can't found a data manger for type " + typeof(U).Name);
        }
        public static void Set(Type type, IGenericDM manager)
        {
            if (dico.ContainsKey(type))
            {
                throw new Exception("A manager already exists for type " + type.Name);
            }
            dico[type] = manager;
        }
    }
    public abstract class GenericDataManager<T, U> : IGenericDM<U> where T : IGenericDM<U>, new() where U : IStorable
    {
        #region singleton
        private static readonly Mutex mutexGetInstance = new Mutex();
        private static readonly Dictionary<Type, T> instances = new Dictionary<Type, T>();
        ///// <summary>
        ///// Singleton pattern
        ///// </summary>
        ///// <returns></returns>
        public static T getInstance()
        {
            mutexGetInstance.WaitOne();
            if (!instances.ContainsKey(typeof(T)))
            {
                instances.Add(typeof(T), new T());
            }
            mutexGetInstance.ReleaseMutex();
            return instances[typeof(T)];
        }
        #endregion

        #region definition
        public Type getMainType()
        {
            return typeof(U);
        }
        public virtual List<Type> defineManualDependances()
        {
            return new List<Type>();
        }
        public string Name
        {
            get => this.GetType().Name.Split('`')[0] + "<" + typeof(U).Name.Split('`')[0] + ">";
        }
        public bool isInit { get; protected set; }
        #endregion

        protected PyramidInfo pyramidInfo { get; set; }
        private Dictionary<Type, PyramidInfo> pyramidsInfo { get; set; } = new Dictionary<Type, PyramidInfo>();
        protected Type rootType { get; set; }
        protected DataManagerConfig config { get; set; }

        protected GenericDataManager()
        {
        }
        public virtual Task<bool> SetConfiguration(PyramidInfo pyramid, DataManagerConfig config)
        {
            pyramidInfo = pyramid;
            pyramidsInfo[pyramid.type] = pyramid;
            if (pyramid.aliasType != null)
            {
                pyramidsInfo[pyramid.aliasType] = pyramid;
            }
            this.config = config;
            SetDMForType(pyramid, true);
            return Task.FromResult(true);
        }

        private void SetDMForType(PyramidInfo pyramid, bool isRoot)
        {
            if (!pyramid.isForceInherit || !isRoot)
            {
                if(rootType == null)
                {
                    rootType = pyramid.type;
                }
                GenericDM.Set(pyramid.type, this);
                pyramidsInfo[pyramid.type] = pyramid;
                if (pyramid.aliasType != null)
                {
                    GenericDM.Set(pyramid.aliasType, this);
                    pyramidsInfo[pyramid.aliasType] = pyramid;
                }
            }
            foreach (PyramidInfo child in pyramid.children)
            {
                SetDMForType(child, false);
            }
        }

        public async Task<bool> Init()
        {
            try
            {
                if (await Initialize())
                {
                    isInit = true;
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }
        protected abstract Task<bool> Initialize();



        public abstract List<X> GetAll<X>() where X : U;
        public List<U> GetAll()
        {
            return this.GetAll<U>();
        }

        List<X> IGenericDM.GetAll<X>()
        {
            return InvokeMethod<List<X>, X>();
        }



        protected X InvokeMethod<X, Y>(object[] parameters = null, [CallerMemberName] string name = "")
        {
            if (parameters == null)
            {
                parameters = new object[] { };
            }
            MethodInfo method = this.GetType().GetMethod(name);
            method = method.MakeGenericMethod(typeof(Y));
            return (X)method.Invoke(this, parameters);
        }
    }
}
