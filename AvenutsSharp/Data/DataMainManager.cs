using AventusSharp.Data.Manager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using AventusSharp.Tools;
using System.Threading.Tasks;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Attributes;

namespace AventusSharp.Data
{
    public class DataManagerConfig
    {
        public List<Assembly> searchingAssemblies = new List<Assembly>() { Assembly.GetEntryAssembly() };
        public IStorage defaultStorage;
        public Type defaultDM = typeof(DummyDM<>);
        public bool createExternalData = false;
        public DataManagerConfigLog log = new DataManagerConfigLog();
    }
    public class DataManagerConfigLog
    {
        public bool monitorDataDependances = false;
        public bool monitorManagerAnalyze = false;
        public bool monitorManagerInit = false;
        public bool monitorManagerOrdering = false;
        public bool monitorManagerOrdered = false;
        public bool monitorDataOrdering = false;
        public bool monitorDataOrdered = false;
    }
    public static class DataMainManager
    {
        private static bool registerDone = false;
        static internal Type DefaultDMType { get; private set; }
        public static bool DefineDefaultDM<T>() where T : IGenericDM
        {
            return DefineTypeDM(typeof(T));
        }
        private static bool DefineTypeDM(Type type)
        {
            if (type != null)
            {
                if (type.IsGenericType)
                {
                    type = type.GetGenericTypeDefinition();
                    if (type.GetGenericArguments().Length == 1)
                    {
                        DefaultDMType = type;
                        return true;
                    }
                    else
                    {
                        new DataError(DataErrorCode.DefaultDMGenericType, "Default DM (" + type.Name + ") must have only one generic type").Print();
                    }
                }
                else
                {
                    new DataError(DataErrorCode.DefaultDMGenericType, "Default DM (" + type.Name + ") must be generic with one generic type").Print();
                }
                return false;
            }
            return true;
        }
        public static async Task Register(DataManagerConfig config)
        {
            if (!registerDone)
            {
                if (!DefineTypeDM(config.defaultDM))
                {
                    return;
                }
                registerDone = true;
                await new DataInit(config).Init();
            }

        }



        private class DataInit
        {
            private DataManagerConfig config;
            private List<ManagerInformation> managerInformations = new List<ManagerInformation>();
            private Dictionary<Type, DataInformation> dataInformations = new Dictionary<Type, DataInformation>();
            private List<DataInformation> orderedData = new List<DataInformation>();
            private List<ManagerInformation> orderedManager = new List<ManagerInformation>();
            private List<DataMemberInfo> storableMembersInfo = new List<DataMemberInfo>();

            public DataInit(DataManagerConfig config)
            {
                this.config = config;
                Assembly baseAssembly = Assembly.GetAssembly(typeof(DataInit));
                if (!config.searchingAssemblies.Contains(baseAssembly))
                {
                    config.searchingAssemblies.Insert(0, baseAssembly);
                }
                // load fields
                Type storableType = typeof(Storable<>);
                PropertyInfo[] properties = storableType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (PropertyInfo property in properties)
                {
                    storableMembersInfo.Add(new DataMemberInfo(property));
                }
            }
            public async Task Init()
            {
                if (!GetAllManagers())
                {
                    return;
                }
                if (!CalculateDataDependances())
                {
                    return;
                }
                if (!OrderData())
                {
                    return;
                }
                if (!MergeManager())
                {
                    return;
                }
                if (!OrderedManager())
                {
                    return;
                }
                if (!await InitManager())
                {
                    return;
                }
            }

            private bool GetAllManagers()
            {
                Dictionary<Type, ManagerInformation> managerInformations = new Dictionary<Type, ManagerInformation>();
                bool monitor = this.config.log.monitorManagerAnalyze;
                if (monitor)
                {
                    Console.WriteLine("*********** Analyze managers **********");
                }
                Stopwatch time = null;
                List<Type> managerTypes = new List<Type>();

                foreach (Assembly assembly in this.config.searchingAssemblies)
                {
                    managerTypes.AddRange(assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IGenericDM)) && !type.IsAbstract && !type.IsGenericTypeDefinition).ToList());
                }

                Action<Type, Type, string> addDependances = delegate (Type typeFrom, Type typeDependance, string name)
                {
                    if (!managerInformations[typeFrom].dependances.ContainsKey(typeDependance))
                    {
                        managerInformations[typeFrom].dependances[typeDependance] = new List<string>();
                    }
                    if (!managerInformations[typeFrom].dependances[typeDependance].Contains(name))
                    {
                        managerInformations[typeFrom].dependances[typeDependance].Add(name);
                    }
                };


                foreach (Type managerType in managerTypes)
                {
                    if (monitor)
                    {
                        time = new Stopwatch();
                        time.Restart();
                    }

                    MethodInfo getInstance = managerType.GetMethod("getInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    IGenericDM manager = (IGenericDM)getInstance.Invoke(null, null);
                    ManagerInformation info = new ManagerInformation()
                    {
                        manager = manager,
                        dependances = new Dictionary<Type, List<string>>(),
                    };

                    info.AddDataInformation(new DataInformation()
                    {
                        data = manager.getMainType(),
                        dependances = new Dictionary<Type, List<string>>(),
                        membersInfo = new Dictionary<string, DataMemberInfo>()

                    });
                    managerInformations.Add(managerType, info);
                    if (monitor)
                    {
                        Console.WriteLine(managerType.Name);
                    }


                    List<Type> manualDependances = manager.defineManualDependances();
                    foreach (Type manualDependance in manualDependances)
                    {
                        if (manualDependance.GetInterfaces().Contains(typeof(IStorable)))
                        {
                            addDependances(managerType, manualDependance, "*manual");
                        }
                        else
                        {
                            new DataError(DataErrorCode.TypeNotStorable, "type " + manualDependance.Name + " is not storable, so you can't add it inside manual dependances of manager " + manager.Name).Print();
                        }
                    }

                }


                this.managerInformations = managerInformations.Values.ToList();
                return true;
            }
            private void AddDataDependance(Type typeFrom, Type typeDependance, string name)
            {
                if (typeDependance == typeof(Object))
                {
                    return;
                }
                if (typeFrom.IsGenericType)
                {
                    typeFrom.GetGenericTypeDefinition();
                }
                if (typeDependance.IsGenericType)
                {
                    typeDependance = typeDependance.GetGenericTypeDefinition();
                }

                if (!dataInformations[typeFrom].dependances.ContainsKey(typeDependance))
                {
                    dataInformations[typeFrom].dependances[typeDependance] = new List<string>();
                }
                if (!dataInformations[typeFrom].dependances[typeDependance].Contains(name))
                {
                    dataInformations[typeFrom].dependances[typeDependance].Add(name);
                }
            }
            private bool CalculateDataDependances()
            {
                bool monitor = this.config.log.monitorDataDependances;
                if (monitor)
                {
                    Console.WriteLine("*********** Calculate data dependances **********");
                }
                List<Type> dataTypes = new List<Type>();
                foreach (Assembly assembly in this.config.searchingAssemblies)
                {
                    dataTypes.AddRange(assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IStorable)) && (type.IsClass || type.IsInterface)).ToList());
                }
                dataTypes.Insert(0, typeof(IStorable));

                foreach (Type dataType in dataTypes)
                {
                    if (!this.CalculateDataDependance(dataType))
                    {
                        return false;
                    }
                }

                return true;
            }
            private bool CalculateDataDependance(Type dataType)
            {
                if (dataInformations.ContainsKey(dataType))
                {
                    return true;
                }
                DataInformation info = new DataInformation()
                {
                    data = dataType,
                    dependances = new Dictionary<Type, List<string>>(),
                    membersInfo = new Dictionary<string, DataMemberInfo>()

                };
                dataInformations.Add(dataType, info);

                // set Data info inside Manager info
                ManagerInformation managerInfo = GetManager(info);
                if (managerInfo == null)
                {
                    return false;
                }
                managerInfo.AddDataInformation(info);
                info.managerInfo = managerInfo;
                if (dataType.IsGenericType)
                {
                    if (!dataType.IsAbstract && !dataType.IsInterface)
                    {
                        new DataError(DataErrorCode.GenericNotAbstract, "You class generic " + info.Name + " must be set as abstract").Print();
                        return false;
                    }
                    Type interfaceType = GetTypeForGenericClass(dataType);
                    if (interfaceType != null)
                    {
                        AddDataDependance(dataType, interfaceType, "*constraint");
                        if (dataInformations.ContainsKey(interfaceType))
                        {
                            dataInformations[interfaceType].genericType = dataType;
                        }
                        else
                        {
                            CalculateDataDependance(interfaceType);
                            dataInformations[interfaceType].genericType = dataType;
                        }
                    }
                }
                else if (dataType.IsAbstract && dataType.IsClass)
                {
                    new DataError(DataErrorCode.UnknowError, "How did you do that for " + dataType.Name).Print();
                }

                // load parent
                Type parentType = dataType.BaseType;
                if (parentType == null || parentType == typeof(Object)) { }
                else if (parentType.IsInterface)
                {
                    AddDataDependance(dataType, parentType, "*parent");
                }
                else if (parentType.IsAbstract && parentType.IsGenericType)
                {
                    AddDataDependance(dataType, parentType, "*parent");
                }
                else
                {
                    new DataError(DataErrorCode.ParentNotAbstract, "A parent must be abstract and generic " + parentType.Name).Print();
                }

                //load current interface
                Type[] interfaces = dataType.GetCurrentInterfaces();
                foreach (Type _interface in interfaces)
                {
                    if (_interface.GetInterfaces().Contains(typeof(IStorable)))
                    {
                        AddDataDependance(dataType, _interface, "*interface");
                    }
                }

                // load fields
                FieldInfo[] fields = dataType.GetFields();
                foreach (FieldInfo field in fields)
                {
                    DataMemberInfo memberInfo = new DataMemberInfo(field);
                    if (!TypeTools.primitiveType.Contains(memberInfo.Type))
                    {
                        AddDataDependance(dataType, memberInfo.Type, memberInfo.Name);
                    }
                    info.membersInfo.Add(memberInfo.Name, memberInfo);
                }
                PropertyInfo[] properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (PropertyInfo property in properties)
                {
                    DataMemberInfo memberInfo = new DataMemberInfo(property);
                    if (!TypeTools.primitiveType.Contains(memberInfo.Type))
                    {
                        AddDataDependance(dataType, memberInfo.Type, memberInfo.Name);
                    }
                    info.membersInfo.Add(memberInfo.Name, memberInfo);
                }

                if (this.config.log.monitorDataDependances)
                {
                    Console.WriteLine(info.ToString());
                }
                return true;
            }
            private bool OrderData()
            {
                bool monitor = this.config.log.monitorDataOrdering;
                List<DataInformation> infos = dataInformations.Values.ToList();
                Stopwatch time = null;
                orderedData = new List<DataInformation>();
                if (monitor)
                {
                    Console.WriteLine("*********** Data ordering **********");
                }
                foreach (DataInformation info in infos)
                {
                    if (monitor)
                    {
                        time = new Stopwatch();
                        time.Restart();
                    }
                    if (OrderDataLoop(info) == -2)
                    {
                        return false;
                    }
                    if (monitor)
                    {
                        Console.WriteLine("Data " + info.Name + " ordering loop in " + time.ElapsedMilliseconds + "ms");
                        time.Stop();
                        time = null;
                    }
                }
                if (this.config.log.monitorDataOrdered)
                {
                    if (monitor)
                    {
                        Console.WriteLine("*********** Data ordered **********");
                    }
                    int i = 1;
                    foreach (DataInformation dataInfo in orderedData)
                    {
                        Console.WriteLine(i + ". " + dataInfo.Name + " - " + dataInfo.managerInfo.manager.Name);
                        i++;
                    }
                }

                this.ForceIStorableFirst();
                return true;
            }
            private void ForceIStorableFirst()
            {
                orderedData.Remove(dataInformations[typeof(IStorable)]);
                orderedData.Insert(0, dataInformations[typeof(IStorable)]);
            }


            private int OrderDataLoop(DataInformation dataInformation, List<DataInformation> waitingData = null)
            {
                if (waitingData == null)
                {
                    waitingData = new List<DataInformation>();
                }

                if (waitingData.Contains(dataInformation))
                {
                    if (waitingData[waitingData.Count - 1] == dataInformation)
                    {
                        // self referencing no error needed
                        return -1;
                    }
                    string msgError = "Infinite loop found for Data " + dataInformation.Name + "\n";
                    msgError += "Elements in loop : \r\n\t - " + string.Join("\r\n\t - ", waitingData.Select(d => d.Name)) + "\r\n\t - " + dataInformation.Name;
                    new DataError(DataErrorCode.InfiniteLoop, msgError).Print();
                    return -2;
                }

                try
                {
                    int indexOfRam = orderedData.IndexOf(dataInformation);
                    if (indexOfRam != -1)
                    {
                        return indexOfRam + 1;
                    }
                    int insertIndex = 0;

                    waitingData.Add(dataInformation);

                    List<Type> dependances = dataInformation.dependances.Keys.ToList();
                    foreach (Type dependanceType in dependances)
                    {
                        if (dataInformations.ContainsKey(dependanceType))
                        {
                            int insertIndexTemp = OrderDataLoop(dataInformations[dependanceType], waitingData);
                            if (insertIndexTemp == -2)
                            {
                                // its an error
                                return -2;
                            }
                            if (insertIndexTemp >= 0 && insertIndexTemp > insertIndex)
                            {
                                insertIndex = insertIndexTemp;
                            }
                        }
                    }
                    waitingData.Remove(dataInformation);

                    if (orderedData.Contains(dataInformation))
                    {
                        new DataError(DataErrorCode.SelfReferecingDependance, "Self referencing dependances found for Data " + dataInformation.Name).Print();
                        return -2;
                    }
                    orderedData.Insert(insertIndex, dataInformation);

                }
                catch (Exception e)
                {
                    new DataError(DataErrorCode.UnknowError, e).Print();
                }
                return orderedData.Count;
            }
            private bool MergeManager()
            {
                // merge manager
                for (int i = orderedData.Count - 1; i > 0; i--)
                {
                    DataInformation dataInformationToMerge = orderedData.ElementAt(i);
                    for (int j = i - 1; j >= 0; j--)
                    {
                        DataInformation dataInformationToCheck = orderedData.ElementAt(j);
                        if (dataInformationToCheck.data.IsInterface)
                        {
                            if (dataInformationToMerge.data.GetInterfaces().Contains(dataInformationToCheck.data))
                            {
                                if (dataInformationToMerge.managerInfo != dataInformationToCheck.managerInfo)
                                {
                                    if (dataInformationToCheck.isMergeable(this.dataInformations))
                                    {
                                        // do not merge if dummy type => prevent real type to be override by dummy
                                        if (!dataInformationToCheck.managerInfo.IsDummy || dataInformationToMerge.managerInfo.IsDummy)
                                        {
                                            ManagerInformation oldManager = dataInformationToMerge.managerInfo;
                                            dataInformationToMerge.managerInfo = dataInformationToCheck.managerInfo;
                                            dataInformationToMerge.managerInfo.AddDataInformation(dataInformationToMerge);

                                            oldManager.RemoveDataInformation(dataInformationToMerge);
                                            if (!oldManager.isUseful)
                                            {
                                                this.managerInformations.Remove(oldManager);
                                            }
                                        }
                                    }


                                }
                            }
                        }

                    }
                }

                // merge dependances and order data inside managers
                for (int i = 0; i < managerInformations.Count; i++)
                {
                    managerInformations[i].PrepareOrderDataInformation();

                }
                foreach (DataInformation dataInformation in orderedData)
                {
                    dataInformation.managerInfo.AddDataInformation(dataInformation);
                    foreach (KeyValuePair<Type, List<string>> dependance in dataInformation.dependances)
                    {
                        Type dependanceType = dependance.Key;
                        List<string> dependancePaths = dependance.Value;
                        if (!dataInformation.managerInfo.UsedForType(dependanceType))
                        {
                            if (!dataInformation.managerInfo.dependances.ContainsKey(dependanceType))
                            {
                                dataInformation.managerInfo.dependances.Add(dependanceType, new List<string>());
                            }
                            foreach (string dependancePath in dependancePaths)
                            {
                                if (!dataInformation.managerInfo.dependances[dependanceType].Contains(dependancePath))
                                {
                                    dataInformation.managerInfo.dependances[dependanceType].Add(dependancePath);
                                }
                            }
                        }
                    }
                }

                return true;
            }
            private bool OrderedManager()
            {
                bool monitor = this.config.log.monitorManagerOrdering;
                Stopwatch time = null;
                orderedManager = new List<ManagerInformation>();
                if (monitor)
                {
                    Console.WriteLine("*********** Manager ordering **********");
                }
                foreach (ManagerInformation info in managerInformations)
                {
                    if (monitor)
                    {
                        time = new Stopwatch();
                        time.Restart();
                    }
                    if (OrderManagerLoop(info) == -2)
                    {
                        return false;
                    }
                    if (monitor)
                    {
                        Console.WriteLine("Manager " + info.manager.Name + " ordering loop in " + time.ElapsedMilliseconds + "ms");
                        time.Stop();
                        time = null;
                    }
                }
                if (this.config.log.monitorManagerOrdered)
                {
                    if (monitor)
                    {
                        Console.WriteLine("*********** Manager ordered **********");
                    }
                    int i = 1;
                    foreach (ManagerInformation managerInfo in orderedManager)
                    {
                        Console.WriteLine(i + ". " + managerInfo.manager.Name);
                        i++;
                    }
                }
                return true;
            }
            private int OrderManagerLoop(ManagerInformation managerInformation, List<ManagerInformation> waitingData = null)
            {
                if (waitingData == null)
                {
                    waitingData = new List<ManagerInformation>();
                }

                if (waitingData.Contains(managerInformation))
                {
                    if (waitingData[waitingData.Count - 1] == managerInformation)
                    {
                        // self referencing no error needed
                        return -1;
                    }
                    string msgError = "Infinite loop found for Manager " + managerInformation.manager.Name + "\n";
                    msgError += "Elements in loop : \r\n\t- " + string.Join("\r\n\t- ", waitingData.Select(d => d.manager.Name)) + "\r\n\t- " + managerInformation.manager.Name;
                    new DataError(DataErrorCode.InfiniteLoop, msgError).Print();
                    return -2;
                }

                try
                {
                    int indexOfRam = orderedManager.IndexOf(managerInformation);
                    if (indexOfRam != -1)
                    {
                        return indexOfRam + 1;
                    }
                    int insertIndex = 0;

                    waitingData.Add(managerInformation);

                    List<Type> dependances = managerInformation.dependances.Keys.ToList();
                    foreach (Type dependanceType in dependances)
                    {
                        if (dataInformations.ContainsKey(dependanceType))
                        {
                            int insertIndexTemp = OrderManagerLoop(dataInformations[dependanceType].managerInfo, waitingData);
                            if (insertIndexTemp == -2)
                            {
                                // its an error
                                return -2;
                            }
                            if (insertIndexTemp >= 0 && insertIndexTemp > insertIndex)
                            {
                                insertIndex = insertIndexTemp;
                            }
                        }
                    }
                    waitingData.Remove(managerInformation);

                    if (orderedManager.Contains(managerInformation))
                    {
                        new DataError(DataErrorCode.SelfReferecingDependance, "Self referencing dependance found for Manager " + managerInformation.manager.Name).Print();
                        return -2;
                    }
                    orderedManager.Insert(insertIndex, managerInformation);

                }
                catch (Exception e)
                {
                    new DataError(DataErrorCode.UnknowError, e).Print();
                }
                return orderedManager.Count;
            }
            private async Task<bool> InitManager()
            {
                bool monitor = this.config.log.monitorManagerInit;
                if (monitor)
                {
                    Console.WriteLine("*********** Init managers **********");
                }
                Stopwatch time = null;
                List<IGenericDM> genericDMs = new List<IGenericDM>();
                foreach (ManagerInformation managerInformation in orderedManager)
                {
                    if (!managerInformation.CanBeInit(true))
                    {
                        continue;
                    }
                    IGenericDM manager = managerInformation.manager;
                    if (!genericDMs.Contains(manager))
                    {
                        genericDMs.Add(manager);
                        PyramidInfo pyramid = CreatePyramid(managerInformation);
                        if (pyramid == null)
                        {
                            return false;
                        }
                        if (!await manager.SetConfiguration(pyramid, this.config))
                        {
                            return false;
                        }
                    }
                }
                foreach (IGenericDM dm in genericDMs)
                {
                    if (monitor)
                    {
                        time = new Stopwatch();
                        time.Restart();
                    }
                    if (!await dm.Init())
                    {
                        return false;
                    }
                    if (monitor)
                    {
                        Console.WriteLine("Manager " + dm.Name + " init in " + time.ElapsedMilliseconds + "ms");
                        time.Stop();
                        time = null;
                    }
                }
                return true;
            }

            private PyramidInfo CreatePyramidStep(DataInformation dataInformation, Dictionary<Type, PyramidInfo> pyramidFloors, Dictionary<Type, string> aliasUsed)
            {
                if (!dataInformation.data.IsInterface)
                {
                    Type constraint = null;
                    Type parent = null;
                    PyramidInfo result;
                    foreach (KeyValuePair<Type, List<string>> pair in dataInformation.dependances)
                    {
                        if (pair.Value.Contains("*constraint"))
                        {
                            constraint = pair.Key;
                            if (!aliasUsed.ContainsKey(constraint))
                            {
                                aliasUsed[constraint] = dataInformation.Name;
                            }
                            else
                            {
                                string msgError = "You can't use the constraint " + constraint.Name + " inside class " + dataInformation.Name + " because interface is still referencing by " + aliasUsed[constraint] + ". You must create an interface for this abstract type.";
                                throw new DataError(DataErrorCode.InterfaceNotUnique, msgError).GetException();
                            }
                        }
                        else if (pair.Value.Contains("*parent"))
                        {
                            parent = pair.Key;
                        }
                    }
                    PyramidInfo info = new PyramidInfo()
                    {
                        type = dataInformation.data,
                        children = new List<PyramidInfo>(),
                        memberInfo = dataInformation.membersInfo.Values.ToList(),
                        aliasType = constraint,
                        dependances = dataInformation.dependances,
                    };
                    result = info;
                    if (parent != null)
                    {
                        if (pyramidFloors.ContainsKey(parent))
                        {
                            info.parent = pyramidFloors[parent];
                            info.parent.children.Add(info);
                        }
                        else if (dataInformations.ContainsKey(parent) && dataInformations[parent].IsForceInherit)
                        {
                            result = CreatePyramidStep(dataInformations[parent], pyramidFloors, aliasUsed);
                            result.isForceInherit = true;
                            if (pyramidFloors.ContainsKey(parent))
                            {
                                info.parent = pyramidFloors[parent];
                                info.parent.children.Add(info);
                            }
                            else
                            {
                                throw new DataError(DataErrorCode.UnknowError, "Somthing went wrong when creating pyramid but I don't know why").GetException();
                            }
                        }
                        else
                        {
                            throw new DataError(DataErrorCode.UnknowError, "Somthing went wrong when creating pyramid but I don't know why").GetException();
                        }
                    }
                    pyramidFloors[dataInformation.data] = info;
                    return result;
                }
                return null;
            }
            private PyramidInfo CreatePyramid(ManagerInformation managerInformation)
            {
                PyramidInfo root = null;

                Dictionary<Type, PyramidInfo> pyramidFloors = new Dictionary<Type, PyramidInfo>();
                Dictionary<Type, string> aliasUsed = new Dictionary<Type, string>(); // interface type, class name
                // at this point data inforamtion is ordered but it's a mix of class, abstract class and interface
                foreach (DataInformation dataInformation in managerInformation.GetDataInformation())
                {
                    try
                    {
                        PyramidInfo info = CreatePyramidStep(dataInformation, pyramidFloors, aliasUsed);
                        if (root == null)
                        {
                            root = info;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        return null;
                    }
                }
                return root;
            }
            private ManagerInformation GetManager(DataInformation dataInformation)
            {
                Type dataType = dataInformation.data;
                if (dataType.IsGenericType)
                {
                    // type must be the interface
                    dataType = GetTypeForGenericClass(dataType);
                    if (dataType == null)
                    {
                        return null;
                    }
                }

                List<ManagerInformation> managers = managerInformations.ToList();
                foreach (ManagerInformation manager in managers)
                {
                    if (manager.UsedForType(dataType))
                    {
                        return manager;
                    }
                }

                Type simpleType = DataMainManager.DefaultDMType.MakeGenericType(new Type[] { dataType });
                MethodInfo getInstance = simpleType.GetMethod("getInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                IGenericDM simpleManager = (IGenericDM)getInstance.Invoke(null, null);

                ManagerInformation info = new ManagerInformation()
                {
                    manager = simpleManager,
                    dependances = new Dictionary<Type, List<string>>(),
                    IsDummy = true,
                };
                info.AddDataInformation(dataInformation);
                managerInformations.Add(info);
                return info;
            }

            /// <summary>
            /// Always the first generic parameter for an abstract class
            /// </summary>
            /// <param name="dataType"></param>
            /// <returns></returns>
            private Type GetTypeForGenericClass(Type dataType)
            {
                if (dataType.IsGenericType)
                {
                    Dictionary<string, Type> genericConstraintMapping = new Dictionary<string, Type>();
                    Type genericType = dataType.GetGenericTypeDefinition();
                    Type[] realValueConstraints = dataType.GetGenericArguments();
                    Type[] genericValueConstraints = genericType.GetGenericArguments();
                    for (int i = 0; i < genericValueConstraints.Length; i++)
                    {
                        genericConstraintMapping.Add(genericValueConstraints[i].ToString(), realValueConstraints[i]);
                    }
                    Type[] def = genericType.GetGenericArguments();
                    Type tp = def[0];
                    Type[] tpConstraints = tp.GetGenericParameterConstraints();
                    List<Type> resultType = new List<Type>();
                    foreach (Type tpc in tpConstraints)
                    {
                        Type constraintType = tpc;
                        if (tpc.IsInterface)
                        {
                            constraintType = TransformTypeGenericToReal(tpc, genericConstraintMapping);
                        }
                        if (constraintType == typeof(IStorable) || constraintType.GetInterfaces().Contains(typeof(IStorable)))
                        {
                            resultType.Add(constraintType);
                        }
                    }

                    if (resultType.Count == 0)
                    {
                        string msgError = "You need to define a constraint IStorable for the type " + tp.Name + " of the class " + dataType.Name;
                        new DataError(DataErrorCode.TypeNotStorable, msgError).Print();
                        return null;
                    }
                    else if (resultType.Count > 1)
                    {
                        string msgError = "Too many constraints IStorable (" + string.Join(", ", resultType.Select(p => p.Name)) + ") for the type " + tp.Name + " of the class " + dataType.Name;
                        new DataError(DataErrorCode.TypeTooMuchStorable, msgError).Print();
                        return null;
                    }
                    else
                    {
                        return resultType[0];
                    }

                }
                return dataType;
            }

            private Type TransformTypeGenericToReal(Type type, Dictionary<string, Type> genericConstraintMapping)
            {
                Type result = type;
                if (type.IsGenericType)
                {

                    Type[] args = type.GetGenericArguments();
                    Type[] typesToUse = new Type[args.Length];
                    for (int i = 0; i < args.Length; i++)
                    {
                        try
                        {
                            if (args[i].IsGenericParameter)
                            {
                                typesToUse[i] = genericConstraintMapping[args[i].ToString()];
                            }
                            else if (args[i].IsGenericType)
                            {
                                typesToUse[i] = TransformTypeGenericToReal(args[i], genericConstraintMapping);
                            }
                            else
                            {
                                typesToUse[i] = args[i];
                            }
                        }
                        catch (Exception e)
                        {
                            new DataError(DataErrorCode.UnknowError, e).Print();
                            new DataError(DataErrorCode.UnknowError, args[i].ToString() + " on constraint " + type.Name + " on type " + type.Name).Print();
                        }
                    }
                    result = type.GetGenericTypeDefinition().MakeGenericType(typesToUse);
                }
                return result;
            }
        }



        private class DataInformation
        {
            public Type data { get; set; }

            // set if data is interface
            public Type genericType { get; set; }
            public string Name
            {
                get
                {
                    return TypeTools.GetReadableName(data);
                }
            }
            public bool isMergeable(Dictionary<Type, DataInformation> dataInformations)
            {
                if (data.IsInterface)
                {
                    if (genericType != null && dataInformations.ContainsKey(genericType))
                    {
                        return dataInformations[genericType].isMergeable(dataInformations);
                    }
                    return false;
                }
                else
                {
                    if (IsForceInherit)
                    {
                        foreach (KeyValuePair<Type, List<string>> dependance in dependances)
                        {
                            if (dependance.Value.Contains("*parent") && dataInformations.ContainsKey(dependance.Key))
                            {
                                return dataInformations[dependance.Key].isMergeable(dataInformations);
                            }
                        }
                        return false;
                    }
                    return true;
                }

            }
            public bool IsForceInherit
            {
                get => data.GetCustomAttributes(false).ToList().Exists(a => a is ForceInherit);
            }
            public ManagerInformation managerInfo;
            public Dictionary<Type, List<string>> dependances = new Dictionary<Type, List<string>>();
            public Dictionary<string, DataMemberInfo> membersInfo = new Dictionary<string, DataMemberInfo>();
            public override string ToString()
            {
                string txt = "**********************\r\n";
                txt += "Data " + Name + "\r\n";
                txt += "Has members :\r\n";
                foreach (KeyValuePair<string, DataMemberInfo> pair in membersInfo)
                {
                    txt += "\t" + pair.Value.ToString() + "\r\n";
                }
                txt += "Has dependanceTypes with :\r\n";
                foreach (KeyValuePair<Type, List<string>> pair in dependances)
                {
                    txt += "\t" + pair.Key.Name + " (" + pair.Key.Assembly.GetName().Name + ")\r\n";
                    foreach (string path in pair.Value)
                    {
                        txt += "\t\t" + path + "\r\n";
                    }
                }
                txt += "**********************\r\n";
                return txt;
            }
        }

        private class ManagerInformation
        {
            public IGenericDM manager;
            public Dictionary<Type, List<string>> dependances = new Dictionary<Type, List<string>>();
            public Dictionary<Type, DataInformation> dataInformations = new Dictionary<Type, DataInformation>();

            public bool IsDummy
            {
                get; set;
            }
            private Dictionary<Type, Type> interfaceForType = new Dictionary<Type, Type>(); // first is interface

            /**
             * Can be init only if there is at least one class not in force inherit
             */
            public bool CanBeInit(bool printError)
            {
                foreach (DataInformation info in dataInformations.Values)
                {
                    if (!info.data.IsInterface && !info.IsForceInherit)
                    {
                        return true;
                    }
                }
                if (printError)
                {
                    if (!dataInformations.ContainsKey(typeof(IStorable)) || dataInformations.Count != 2)
                    {
                        string msgError = "Can't init " + manager.Name + " because only contains ForceInehrit elements";
                        new DataError(DataErrorCode.DMOnlyForceInherit, msgError).Print();
                    }
                }
                return false;
            }

            public void AddDataInformation(DataInformation dataInformation)
            {
                dataInformations[dataInformation.data] = dataInformation;
            }
            public void RemoveDataInformation(DataInformation dataInformation)
            {
                if (dataInformations.ContainsKey(dataInformation.data))
                {
                    dataInformations.Remove(dataInformation.data);
                }
            }

            public void PrepareOrderDataInformation()
            {
                this.dataInformations = new Dictionary<Type, DataInformation>();
            }
            public List<DataInformation> GetDataInformation()
            {
                return dataInformations.Values.ToList();
            }
            public bool isUseful { get => dataInformations.Count > 0; }
            public bool UsedForType(Type type)
            {
                return dataInformations.ContainsKey(type);
            }
            public string PrintCurrentAssembly()
            {

                string txt = "**********************\r\n";
                txt += "Manager " + manager.Name + "\r\n";
                txt += "Used for types :\r\n";
                foreach (DataInformation data in dataInformations.Values)
                {
                    txt += "\t" + data.Name + "\r\n";
                }


                txt += "Has dependanceTypes with :\r\n";
                foreach (KeyValuePair<Type, List<string>> pair in dependances)
                {
                    if (pair.Key.Assembly == Assembly.GetEntryAssembly())
                    {
                        txt += "\t" + pair.Key.Name + " (" + pair.Key.Assembly.GetName().Name + ")\r\n";
                        foreach (string path in pair.Value)
                        {
                            txt += "\t\t" + path + "\r\n";
                        }
                    }
                }
                txt += "**********************\r\n";
                return txt;
            }
            public override string ToString()
            {
                string txt = "**********************\r\n";
                txt += "Manager " + manager.Name + "\r\n";
                txt += "Used for types :\r\n";
                foreach (DataInformation data in dataInformations.Values)
                {
                    txt += "\t" + data.Name + "\r\n";
                }


                txt += "Has dependanceTypes with :\r\n";
                foreach (KeyValuePair<Type, List<string>> pair in dependances)
                {
                    txt += "\t" + pair.Key.Name + " (" + pair.Key.Assembly.GetName().Name + ")\r\n";
                    foreach (string path in pair.Value)
                    {
                        txt += "\t\t" + path + "\r\n";
                    }
                }
                txt += "**********************\r\n";
                return txt;
            }
        }

    }

    public class PyramidInfo
    {
        public PyramidInfo parent;
        public List<PyramidInfo> children;

        public Type type;
        public Type aliasType;
        public List<DataMemberInfo> memberInfo;

        public bool isForceInherit = false;

        public Dictionary<Type, List<string>> dependances;
    }

}
