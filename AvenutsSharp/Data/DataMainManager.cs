using AventusSharp.Data.Manager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AventusSharp.Tools;
using System.Threading.Tasks;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Attributes;

namespace AventusSharp.Data
{
    public class DataManagerConfig
    {
        public IDBStorage? defaultStorage;
        public Type defaultDM = typeof(DummyDM<>);
        public bool createExternalData = false;
        public DataManagerConfigLog log = new();
        public bool nullByDefault = false;
        public bool preferLocalCache = false;
        public bool preferShortLink = false;

        public DataManagerConfig()
        {
        }
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
        static internal DataManagerConfig Config { get; private set; } = new DataManagerConfig();
        private static Action<DataManagerConfig> configureAction = (config) => { };
        private static bool registerDone = false;
        static internal Type? DefaultDMType { get; private set; }
        static internal readonly List<Assembly> searchingAssemblies = new();


        private static VoidWithError DefineTypeDM(Type type)
        {
            VoidWithError result = new VoidWithError();
            if (type != null)
            {
                if (type.IsGenericType)
                {
                    type = type.GetGenericTypeDefinition();
                    if (type.GetGenericArguments().Length == 1)
                    {
                        DefaultDMType = type;
                    }
                    else
                    {
                        result.Errors.Add(new DataError(DataErrorCode.DefaultDMGenericType, "Default DM (" + type.Name + ") must have only one generic type"));
                    }
                }
                else
                {
                    result.Errors.Add(new DataError(DataErrorCode.DefaultDMGenericType, "Default DM (" + type.Name + ") must be generic with one generic type"));
                }
            }
            else
            {
                result.Errors.Add(new DataError(DataErrorCode.DefaultDMGenericType, "The type defined is null"));
            }
            return result;
        }
        public static void Configure(Action<DataManagerConfig> config)
        {
            configureAction = config;
        }

        public static Task<VoidWithError> Init()
        {
            List<Assembly> searchingAssemblies = new();
            Assembly? assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                searchingAssemblies.Add(assembly);
            }
            return Init(searchingAssemblies);
        }
        public static Task<VoidWithError> Init(Assembly assembly)
        {
            List<Assembly> searchingAssemblies = new();
            if (assembly != null)
            {
                searchingAssemblies.Add(assembly);
            }
            return Init(searchingAssemblies);
        }

        public static async Task<VoidWithError> Init(List<Assembly> assemblies)
        {
            if (!registerDone)
            {
                configureAction(Config);
                VoidWithError resultTemp = DefineTypeDM(Config.defaultDM);
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                registerDone = true;
            }

            if (MergeAssemblies(assemblies) == 0)
            {
                return new VoidWithError();
            }
            return await new DataInit().Init();
        }

        private static int MergeAssemblies(List<Assembly> assemblies)
        {
            int newDlls = 0;
            Assembly? baseAssembly = Assembly.GetAssembly(typeof(DataInit));
            if (baseAssembly != null && !searchingAssemblies.Contains(baseAssembly))
            {
                newDlls++;
                searchingAssemblies.Insert(0, baseAssembly);
            }

            foreach (Assembly assembly in assemblies)
            {
                if (!searchingAssemblies.Contains(assembly))
                {
                    newDlls++;
                    searchingAssemblies.Add(assembly);
                }
            }
            return newDlls;
        }

        private class DataInit
        {
            private readonly DataManagerConfig config = DataMainManager.Config;
            private List<ManagerInformation> managerInformations = new();
            private readonly Dictionary<Type, DataInformation> dataInformations = new();
            private List<DataInformation> orderedData = new();
            private List<ManagerInformation> orderedManager = new();
            private readonly List<DataMemberInfo> storableMembersInfo = new();


            public DataInit()
            {
                // load fields
                Type storableType = typeof(Storable<>);
                PropertyInfo[] properties = storableType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (PropertyInfo property in properties)
                {
                    storableMembersInfo.Add(new DataMemberInfo(property));
                }
            }
            
            
            public async Task<VoidWithError> Init()
            {
                VoidWithError resultTemp = new VoidWithError();

                
                resultTemp = GetAllManagers();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                resultTemp = CalculateDataDependances();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                resultTemp = OrderData();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                resultTemp = MergeManager();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                resultTemp = OrderedManager();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                resultTemp = await InitManager();
                if (!resultTemp.Success)
                {
                    return resultTemp;
                }
                
                return resultTemp;
            }

            private VoidWithError GetAllManagers()
            {

                VoidWithError result = new VoidWithError();
                Dictionary<Type, ManagerInformation> managerInformations = new();
                bool monitor = this.config.log.monitorManagerAnalyze;
                if (monitor)
                {
                    Console.WriteLine("*********** Analyze managers **********");
                }
                Stopwatch? time = null;
                List<Type> managerTypes = GenericDM.GetExistingDMTypes();

                foreach (Assembly assembly in searchingAssemblies)
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        if(type.GetInterfaces().Contains(typeof(IGenericDM)) && !type.IsAbstract && !type.IsGenericTypeDefinition && !managerTypes.Contains(type))
                        {
                            managerTypes.Add(type);
                        }
                    }
                }

                void addDependances(Type typeFrom, Type typeDependance, string name)
                {
                    if (!managerInformations[typeFrom].dependances.ContainsKey(typeDependance))
                    {
                        managerInformations[typeFrom].dependances[typeDependance] = new List<string>();
                    }
                    if (!managerInformations[typeFrom].dependances[typeDependance].Contains(name))
                    {
                        managerInformations[typeFrom].dependances[typeDependance].Add(name);
                    }
                }


                foreach (Type managerType in managerTypes)
                {
                    if (monitor)
                    {
                        time = new Stopwatch();
                        time.Restart();
                    }
                    
                    MethodInfo? GetInstance = managerType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (GetInstance == null)
                    {
                        result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Manager " + managerType.Name + " doesn't have a GetInstance function"));
                        return result;
                    }
                    IGenericDM? manager = (IGenericDM?)GetInstance.Invoke(null, null);
                    if (manager == null)
                    {
                        continue;
                    }
                    ManagerInformation info = new(manager)
                    {
                        dependances = new Dictionary<Type, List<string>>(),
                    };

                    info.AddDataInformation(new DataInformation()
                    {
                        Data = manager.GetMainType(),
                        dependances = new Dictionary<Type, List<string>>(),
                        membersInfo = new Dictionary<string, DataMemberInfo>()

                    });
                    managerInformations.Add(managerType, info);
                    if (monitor)
                    {
                        Console.WriteLine(managerType.Name);
                    }


                    List<Type> manualDependances = manager.DefineManualDependances();
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
                return result;
            }
            private void AddDataDependance(Type typeFrom, Type? typeDependance, string name)
            {
                if (typeDependance == null || typeDependance == typeof(Object))
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
            private VoidWithError CalculateDataDependances()
            {
                VoidWithError result = new();
                bool monitor = this.config.log.monitorDataDependances;
                if (monitor)
                {
                    Console.WriteLine("*********** Calculate data dependances **********");
                }
                List<Type> dataTypes = new();
                foreach (Assembly assembly in searchingAssemblies)
                {
                    dataTypes.AddRange(assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IStorable)) && (type.IsClass || type.IsInterface)).ToList());
                }
                dataTypes.Insert(0, typeof(IStorable));

                foreach (Type dataType in dataTypes)
                {
                    result = CalculateDataDependance(dataType);
                    if (!result.Success)
                    {
                        return result;
                    }
                }

                return result;
            }
            private VoidWithError CalculateDataDependance(Type dataType)
            {
                VoidWithError result = new();
                if (dataInformations.ContainsKey(dataType))
                {
                    return result;
                }
                DataInformation info = new()
                {
                    Data = dataType,
                    dependances = new Dictionary<Type, List<string>>(),
                    membersInfo = new Dictionary<string, DataMemberInfo>()

                };
                dataInformations.Add(dataType, info);

                // set Data info inside Manager info
                ResultWithError<ManagerInformation> managerInfoResult = GetManager(info);
                if (!managerInfoResult.Success || managerInfoResult.Result == null)
                {
                    result.Errors = managerInfoResult.Errors;
                    return result;
                }
                ManagerInformation managerInfo = managerInfoResult.Result;
                managerInfo.AddDataInformation(info);
                info.ManagerInfo = managerInfo;
                if (dataType.IsGenericType)
                {
                    if (!dataType.IsAbstract && !dataType.IsInterface)
                    {
                        result.Errors.Add(new DataError(DataErrorCode.GenericNotAbstract, "You class generic " + info.Name + " must be set as abstract"));
                        return result;
                    }
                    ResultWithError<Type> interfaceTypeTemp = GetTypeForGenericClass(dataType);
                    if (interfaceTypeTemp.Success && interfaceTypeTemp.Result != null)
                    {
                        Type interfaceType = interfaceTypeTemp.Result;
                        AddDataDependance(dataType, interfaceType, "*constraint");
                        if (dataInformations.ContainsKey(interfaceType))
                        {
                            dataInformations[interfaceType].GenericType = dataType;
                        }
                        else
                        {
                            VoidWithError resultTemp = CalculateDataDependance(interfaceType);
                            if (!resultTemp.Success)
                            {
                                result.Errors.AddRange(resultTemp.Errors);
                                return result;
                            }
                            dataInformations[interfaceType].GenericType = dataType;
                        }
                    }
                }
                else if (dataType.IsAbstract && dataType.IsClass)
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, "How did you do that for " + dataType.Name));
                    return result;
                }

                // load parent
                Type? parentType = dataType.BaseType;
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
                    result.Errors.Add(new DataError(DataErrorCode.ParentNotAbstract, "A parent must be abstract and generic " + parentType.Name));
                    return result;
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
                    DataMemberInfo memberInfo = new(field);
                    if (!TypeTools.PrimitiveType.Contains(memberInfo.Type))
                    {
                        AddDataDependance(dataType, memberInfo.Type, memberInfo.Name);
                    }
                    info.membersInfo.Add(memberInfo.Name, memberInfo);
                }
                PropertyInfo[] properties = dataType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (PropertyInfo property in properties)
                {
                    DataMemberInfo memberInfo = new(property);
                    if (!TypeTools.PrimitiveType.Contains(memberInfo.Type))
                    {
                        AddDataDependance(dataType, memberInfo.Type, memberInfo.Name);
                    }
                    info.membersInfo.Add(memberInfo.Name, memberInfo);
                }

                if (config.log.monitorDataDependances)
                {
                    Console.WriteLine(info.ToString());
                }
                return result;
            }
            private VoidWithError OrderData()
            {
                VoidWithError result = new VoidWithError();
                bool monitor = config.log.monitorDataOrdering;
                List<DataInformation> infos = dataInformations.Values.ToList();
                Stopwatch? time = null;
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
                    ResultWithError<int> resultTemp = OrderDataLoop(info);
                    if (!resultTemp.Success)
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                        return result;
                    }
                    if (monitor && time != null)
                    {
                        Console.WriteLine("Data " + info.Name + " ordering loop in " + time.ElapsedMilliseconds + "ms");
                        time.Stop();
                        time = null;
                    }
                }
                if (config.log.monitorDataOrdered)
                {
                    if (monitor)
                    {
                        Console.WriteLine("*********** Data ordered **********");
                    }
                    int i = 1;
                    foreach (DataInformation dataInfo in orderedData)
                    {
                        Console.WriteLine(i + ". " + dataInfo.Name + " - " + dataInfo.ManagerInfo.Manager.Name);
                        i++;
                    }
                }

                ForceIStorableFirst();
                return result;
            }
            private void ForceIStorableFirst()
            {
                orderedData.Remove(dataInformations[typeof(IStorable)]);
                orderedData.Insert(0, dataInformations[typeof(IStorable)]);
            }


            private ResultWithError<int> OrderDataLoop(DataInformation dataInformation, List<DataInformation>? waitingData = null)
            {
                ResultWithError<int> result = new ResultWithError<int>();
                waitingData ??= new List<DataInformation>();

                if (waitingData.Contains(dataInformation))
                {
                    if (waitingData[^1] == dataInformation)
                    {
                        // self referencing no error needed
                        result.Result = - 1;
                        return result;
                    }
                    string msgError = "Infinite loop found for Data " + dataInformation.Name + "\n";
                    msgError += "Elements in loop : \r\n\t - " + string.Join("\r\n\t - ", waitingData.Select(d => d.Name)) + "\r\n\t - " + dataInformation.Name;
                    result.Errors.Add(new DataError(DataErrorCode.InfiniteLoop, msgError));
                    return result;
                }

                try
                {
                    int indexOfRam = orderedData.IndexOf(dataInformation);
                    if (indexOfRam != -1)
                    {
                        result.Result = indexOfRam + 1;
                        return result;
                    }
                    int insertIndex = 0;

                    waitingData.Add(dataInformation);

                    List<Type> dependances = dataInformation.dependances.Keys.ToList();
                    foreach (Type dependanceType in dependances)
                    {
                        if (dataInformations.ContainsKey(dependanceType))
                        {
                            ResultWithError<int> insertIndexTemp = OrderDataLoop(dataInformations[dependanceType], waitingData);
                            if (!insertIndexTemp.Success)
                            {
                                // its an error
                                return insertIndexTemp;
                            }
                            if (insertIndexTemp.Result >= 0 && insertIndexTemp.Result > insertIndex)
                            {
                                insertIndex = insertIndexTemp.Result;
                            }
                        }
                    }
                    waitingData.Remove(dataInformation);

                    if (orderedData.Contains(dataInformation))
                    {
                        result.Errors.Add(new DataError(DataErrorCode.SelfReferecingDependance, "Self referencing dependances found for Data " + dataInformation.Name));
                        return result;
                    }
                    orderedData.Insert(insertIndex, dataInformation);

                }
                catch (Exception e)
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                }
                result.Result = orderedData.Count;
                return result;
            }
            private VoidWithError MergeManager()
            {
                VoidWithError result = new VoidWithError();
                // merge manager
                for (int i = orderedData.Count - 1; i > 0; i--)
                {
                    DataInformation dataInformationToMerge = orderedData.ElementAt(i);
                    for (int j = i - 1; j >= 0; j--)
                    {
                        DataInformation dataInformationToCheck = orderedData.ElementAt(j);
                        if (dataInformationToCheck.Data.IsInterface)
                        {
                            if (dataInformationToMerge.Data.GetInterfaces().Contains(dataInformationToCheck.Data))
                            {
                                if (dataInformationToMerge.ManagerInfo != dataInformationToCheck.ManagerInfo)
                                {
                                    if (dataInformationToCheck.IsMergeable(this.dataInformations))
                                    {
                                        // do not merge if dummy type => prevent real type to be override by dummy
                                        if (!dataInformationToCheck.ManagerInfo.IsDummy || dataInformationToMerge.ManagerInfo.IsDummy)
                                        {
                                            ManagerInformation oldManager = dataInformationToMerge.ManagerInfo;
                                            dataInformationToMerge.ManagerInfo = dataInformationToCheck.ManagerInfo;
                                            dataInformationToMerge.ManagerInfo.AddDataInformation(dataInformationToMerge);

                                            oldManager.RemoveDataInformation(dataInformationToMerge);
                                            if (!oldManager.IsUseful)
                                            {
                                                managerInformations.Remove(oldManager);
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
                    dataInformation.ManagerInfo.AddDataInformation(dataInformation);
                    foreach (KeyValuePair<Type, List<string>> dependance in dataInformation.dependances)
                    {
                        Type dependanceType = dependance.Key;
                        List<string> dependancePaths = dependance.Value;
                        if (!dataInformation.ManagerInfo.UsedForType(dependanceType))
                        {
                            if (!dataInformation.ManagerInfo.dependances.ContainsKey(dependanceType))
                            {
                                dataInformation.ManagerInfo.dependances.Add(dependanceType, new List<string>());
                            }
                            foreach (string dependancePath in dependancePaths)
                            {
                                if (!dataInformation.ManagerInfo.dependances[dependanceType].Contains(dependancePath))
                                {
                                    dataInformation.ManagerInfo.dependances[dependanceType].Add(dependancePath);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            private VoidWithError OrderedManager()
            {
                VoidWithError result = new();
                bool monitor = this.config.log.monitorManagerOrdering;
                Stopwatch? time = null;
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
                    ResultWithError<int> resultTemp = OrderManagerLoop(info);
                    if (!resultTemp.Success)
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                        return result;
                    }
                    if (time != null)
                    {
                        Console.WriteLine("Manager " + info.Manager.Name + " ordering loop in " + time.ElapsedMilliseconds + "ms");
                        time.Stop();
                        time = null;
                    }
                }
                if (config.log.monitorManagerOrdered)
                {
                    if (monitor)
                    {
                        Console.WriteLine("*********** Manager ordered **********");
                    }
                    int i = 1;
                    foreach (ManagerInformation managerInfo in orderedManager)
                    {
                        Console.WriteLine(i + ". " + managerInfo.Manager.Name);
                        i++;
                    }
                }
                return result;
            }
            private ResultWithError<int> OrderManagerLoop(ManagerInformation managerInformation, List<ManagerInformation>? waitingData = null)
            {
                ResultWithError<int> result = new();
                waitingData ??= new List<ManagerInformation>();

                if (waitingData.Contains(managerInformation))
                {
                    if (waitingData[^1] == managerInformation)
                    {
                        // self referencing no error needed
                        result.Result = -1;
                        return result;
                    }
                    string msgError = "Infinite loop found for Manager " + managerInformation.Manager.Name + "\n";
                    msgError += "Elements in loop : \r\n\t- " + string.Join("\r\n\t- ", waitingData.Select(d => d.Manager.Name)) + "\r\n\t- " + managerInformation.Manager.Name;
                    result.Errors.Add(new DataError(DataErrorCode.InfiniteLoop, msgError));
                    return result;
                }

                try
                {
                    int indexOfRam = orderedManager.IndexOf(managerInformation);
                    if (indexOfRam != -1)
                    {
                        result.Result = indexOfRam + 1;
                        return result;
                    }
                    int insertIndex = 0;

                    waitingData.Add(managerInformation);

                    List<Type> dependances = managerInformation.dependances.Keys.ToList();
                    foreach (Type dependanceType in dependances)
                    {
                        if (dataInformations.ContainsKey(dependanceType))
                        {
                            ManagerInformation? managerInfo = dataInformations[dependanceType].ManagerInfo;
                            if (managerInfo != null)
                            {
                                ResultWithError<int> insertIndexTemp = OrderManagerLoop(managerInfo, waitingData);
                                if (!insertIndexTemp.Success)
                                {
                                    // its an error
                                    return insertIndexTemp;
                                }
                                if (insertIndexTemp.Result >= 0 && insertIndexTemp.Result > insertIndex)
                                {
                                    insertIndex = insertIndexTemp.Result;
                                }
                            }
                        }
                    }
                    waitingData.Remove(managerInformation);

                    if (orderedManager.Contains(managerInformation))
                    {
                        result.Errors.Add(new DataError(DataErrorCode.SelfReferecingDependance, "Self referencing dependance found for Manager " + managerInformation.Manager.Name));
                        return result;
                    }
                    orderedManager.Insert(insertIndex, managerInformation);

                }
                catch (Exception e)
                {
                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                }
                result.Result = orderedManager.Count;
                return result;
            }
            private async Task<VoidWithError> InitManager()
            {
                VoidWithError result = new VoidWithError();
                bool monitor = this.config.log.monitorManagerInit;
                if (monitor)
                {
                    Console.WriteLine("*********** Init managers **********");
                }
                Stopwatch? time = null;
                List<IGenericDM> genericDMs = new();
                foreach (ManagerInformation managerInformation in orderedManager)
                {
                    if (!managerInformation.CanBeInit(true))
                    {
                        continue;
                    }
                    IGenericDM manager = managerInformation.Manager;
                    if (!genericDMs.Contains(manager))
                    {
                        genericDMs.Add(manager);
                        ResultWithError<PyramidInfo> pyramidResult = CreatePyramid(managerInformation);
                        if (!pyramidResult.Success || pyramidResult.Result == null)
                        {
                            result.Errors.AddRange(pyramidResult.Errors);
                            return result;
                        }
                        VoidWithError resultTemp = await manager.SetConfiguration(pyramidResult.Result, this.config);
                        if (!resultTemp.Success)
                        {
                            result.Errors.AddRange(resultTemp.Errors);
                            return result;
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
                    VoidWithError resultTemp = await dm.Init();
                    if (!resultTemp.Success)
                    {
                        result.Errors.AddRange(resultTemp.Errors);
                        return result;
                    }
                    if (time != null)
                    {
                        Console.WriteLine("Manager " + dm.Name + " init in " + time.ElapsedMilliseconds + "ms");
                        time.Stop();
                        time = null;
                    }
                }
                return result;
            }

            private ResultWithError<PyramidInfo> CreatePyramidStep(DataInformation dataInformation, Dictionary<Type, PyramidInfo> pyramidFloors, Dictionary<Type, string> aliasUsed)
            {
                ResultWithError<PyramidInfo> result = new();
                if (!dataInformation.Data.IsInterface)
                {
                    Type? constraint = null;
                    Type? parent = null;
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
                                result.Errors.Add(new DataError(DataErrorCode.InterfaceNotUnique, msgError));
                                return result;
                            }
                        }
                        else if (pair.Value.Contains("*parent"))
                        {
                            parent = pair.Key;
                        }
                    }
                    PyramidInfo info = new(
                        type: dataInformation.Data,
                        memberInfo: dataInformation.membersInfo.Values.ToList()
                    )
                    {
                        dependances = dataInformation.dependances,
                        aliasType = constraint
                    };
                    result.Result = info;
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
                            if (result.Success && result.Result != null)
                            {
                                result.Result.isForceInherit = true;
                                if (pyramidFloors.ContainsKey(parent))
                                {
                                    info.parent = pyramidFloors[parent];
                                    info.parent.children.Add(info);
                                }
                                else
                                {
                                    result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Somthing went wrong when creating pyramid but I don't know why"));
                                    return result;
                                }
                            }
                            else if(result.Errors.Count > 0)
                            {
                                result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Somthing went wrong when creating pyramid but I don't know why"));
                                return result;
                            }
                        }
                        else
                        {
                            result.Errors.Add(new DataError(DataErrorCode.UnknowError, "Somthing went wrong when creating pyramid but I don't know why"));
                            return result;
                        }
                    }
                    pyramidFloors[dataInformation.Data] = info;
                    return result;
                }
                return result;
            }
            private ResultWithError<PyramidInfo> CreatePyramid(ManagerInformation managerInformation)
            {
                ResultWithError<PyramidInfo> result = new();

                Dictionary<Type, PyramidInfo> pyramidFloors = new();
                Dictionary<Type, string> aliasUsed = new(); // interface type, class name
                // at this point data inforamtion is ordered but it's a mix of class, abstract class and interface
                foreach (DataInformation dataInformation in managerInformation.GetDataInformation())
                {
                    try
                    {
                        ResultWithError<PyramidInfo> info = CreatePyramidStep(dataInformation, pyramidFloors, aliasUsed);
                        if(info.Success && info.Result != null)
                        {
                            result.Result ??= info.Result;
                        }
                        else if(info.Errors.Count > 0)
                        {
                            result.Errors.AddRange(info.Errors);
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        result.Errors.Add(new DataError(DataErrorCode.UnknowError, e));
                        return result;
                    }
                }
                return result;
            }
            private ResultWithError<ManagerInformation> GetManager(DataInformation dataInformation)
            {
                ResultWithError<ManagerInformation> result = new();
                Type? dataType = dataInformation.Data;
                if (dataType.IsGenericType)
                {
                    // type must be the interface
                    ResultWithError<Type> resultTemp = GetTypeForGenericClass(dataType);
                    if (!resultTemp.Success || resultTemp.Result == null )
                    {
                        result.Errors = resultTemp.Errors;
                        return result;
                    }
                    dataType = resultTemp.Result;
                }

                List<ManagerInformation> managers = managerInformations.ToList();
                foreach (ManagerInformation manager in managers)
                {
                    if (manager.UsedForType(dataType))
                    {
                        result.Result = manager;
                        return result;
                    }
                }

                if (DefaultDMType == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.DMNotExist, "You must define a default DM"));
                    return result;
                }
                Type simpleType = DefaultDMType.MakeGenericType(new Type[] { dataType });
                MethodInfo? GetInstance = simpleType.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (GetInstance == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.MethodNotFound, "Your default DM doesn't have a method GetInstance"));
                    return result;
                }
                IGenericDM? simpleManager = (IGenericDM?)GetInstance.Invoke(null, null);
                if (simpleManager == null)
                {
                    result.Errors.Add(new DataError(DataErrorCode.DMNotExist, "The methode GetInstance inside your default DM doesn't return a Generic DM"));
                    return result;
                }
                ManagerInformation info = new(simpleManager)
                {
                    dependances = new Dictionary<Type, List<string>>(),
                    IsDummy = true,
                };
                info.AddDataInformation(dataInformation);
                managerInformations.Add(info);
                result.Result = info;
                return result;
            }

            /// <summary>
            /// Always the first generic parameter for an abstract class
            /// </summary>
            /// <param name="dataType"></param>
            /// <returns></returns>
            private ResultWithError<Type> GetTypeForGenericClass(Type dataType)
            {
                ResultWithError<Type> result = new();
                if (dataType.IsGenericType)
                {
                    Dictionary<string, Type> genericConstraintMapping = new();
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
                    List<Type> resultType = new();
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
                        result.Errors.Add(new DataError(DataErrorCode.TypeNotStorable, msgError));
                        return result;
                    }
                    else if (resultType.Count > 1)
                    {
                        string msgError = "Too many constraints IStorable (" + string.Join(", ", resultType.Select(p => p.Name)) + ") for the type " + tp.Name + " of the class " + dataType.Name;
                        result.Errors.Add(new DataError(DataErrorCode.TypeTooMuchStorable, msgError));
                        return result;
                    }
                    else
                    {
                        result.Result = resultType[0];
                    }
                }
                else
                {
                    result.Result = dataType;
                }
                return result;
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
#pragma warning disable CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
            public Type Data { get; set; }
#pragma warning restore CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.

            // set if data is interface
            public Type? GenericType { get; set; }
            public string Name
            {
                get
                {
                    return TypeTools.GetReadableName(Data);
                }
            }
            public bool IsMergeable(Dictionary<Type, DataInformation> dataInformations)
            {
                if (Data.IsInterface)
                {
                    if (GenericType != null && dataInformations.ContainsKey(GenericType))
                    {
                        return dataInformations[GenericType].IsMergeable(dataInformations);
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
                                return dataInformations[dependance.Key].IsMergeable(dataInformations);
                            }
                        }
                        return false;
                    }
                    return true;
                }

            }
            public bool IsForceInherit
            {
                get => Data.GetCustomAttributes(false).ToList().Exists(a => a is ForceInherit);
            }

#pragma warning disable CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
            public ManagerInformation ManagerInfo { get; set; }
#pragma warning restore CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Envisagez de déclarer le champ comme nullable.
            public Dictionary<Type, List<string>> dependances = new();
            public Dictionary<string, DataMemberInfo> membersInfo = new();
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
            public IGenericDM Manager { get; private set; }
            public Dictionary<Type, List<string>> dependances = new();
            public Dictionary<Type, DataInformation> dataInformations = new();

            public bool IsDummy
            {
                get; set;
            }

            public ManagerInformation(IGenericDM manager)
            {
                this.Manager = manager;
            }

            /**
             * Can be init only if there is at least one class not in force inherit
             */
            public bool CanBeInit(bool printError)
            {
                foreach (DataInformation info in dataInformations.Values)
                {
                    if (!info.Data.IsInterface && !info.IsForceInherit)
                    {
                        return true;
                    }
                }
                if (printError)
                {
                    if (!dataInformations.ContainsKey(typeof(IStorable)) || dataInformations.Count != 2)
                    {
                        string msgError = "Can't init " + Manager.Name + " because only contains ForceInehrit elements";
                        new DataError(DataErrorCode.DMOnlyForceInherit, msgError).Print();
                    }
                }
                return false;
            }

            public void AddDataInformation(DataInformation dataInformation)
            {
                dataInformations[dataInformation.Data] = dataInformation;
            }
            public void RemoveDataInformation(DataInformation dataInformation)
            {
                if (dataInformations.ContainsKey(dataInformation.Data))
                {
                    dataInformations.Remove(dataInformation.Data);
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
            public bool IsUseful { get => dataInformations.Count > 0; }
            public bool UsedForType(Type type)
            {
                return dataInformations.ContainsKey(type);
            }
            public string PrintCurrentAssembly()
            {

                string txt = "**********************\r\n";
                txt += "Manager " + Manager.Name + "\r\n";
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
                txt += "Manager " + Manager.Name + "\r\n";
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
        public PyramidInfo? parent;
        public List<PyramidInfo> children = new();

        public Type type;
        public Type? aliasType;
        public List<DataMemberInfo> memberInfo = new();

        public bool isForceInherit = false;

        public Dictionary<Type, List<string>> dependances = new();

        public PyramidInfo(Type type, List<DataMemberInfo> memberInfo)
        {
            this.type = type;
            this.memberInfo = memberInfo;
        }
    }

}
