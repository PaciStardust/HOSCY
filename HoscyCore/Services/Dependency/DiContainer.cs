using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCore.Services.Dependency;

/// <summary>
/// Container for resolving and providing dependencies, and starting and stopping services
/// </summary>
public class DiContainer
{
    public ServiceProvider Services { get; init; }
    private readonly ILogger _logger;

    #region Constructor
    private DiContainer(ServiceProvider provider, ILogger logger)
    {
        Services = provider;
        _logger = logger.ForContext<DiContainer>();
    }

    /// <summary>
    /// Creates an empty DiContainer for debugging
    /// </summary>
    public static DiContainer Empty()
    {
        return new DiContainer
        (
            new ServiceCollection().BuildServiceProvider(),
            new LoggerConfiguration().CreateLogger()
        );
    }

    /// <summary>
    /// Creates a DiContainer using a Logger, ConfigModel, some manual extra additions and all classes with the loader attribute
    /// </summary>
    /// <param name="logger">Logger for container</param>
    /// <param name="config">ConfigModel for container</param>
    /// <param name="additionalInserts">An action to insert additional dependencies manually</param>
    /// <returns>DiContainer with all dependencies loaded in</returns>
    public static Res<DiContainer> CreateWithAssembly(ILogger logger, ConfigModel config, Action<ServiceCollection>? additionalInserts = null)
    {
        var diLogger = logger.ForContext<DiContainer>();
        var sw = Stopwatch.StartNew();
        diLogger.Information("Creating DiContainer with assembly...");

        var collection = new ServiceCollection();
        collection.AddSingleton(diLogger)
            .AddSingleton(config);

        var fillRes = FillCollectionWithAssembly(collection, diLogger);
        if (!fillRes.IsOk) return ResC.TFail<DiContainer>(fillRes.Msg);

        var addRes = ResC.WrapR(() => additionalInserts?.Invoke(collection), "Failed to handle additional inserts", diLogger);
        return ResC.TOk(new DiContainer(collection.BuildServiceProvider(), diLogger));
    }
    #endregion

    #region Retrieving Services
    /// <summary>
    /// Shortcut for Services.GetService
    /// </summary>
    public T? GetService<T>()
    {
        return Services.GetService<T>();
    }

    /// <summary>
    /// Shortcut for Services.GetRequiredService
    /// </summary>
    public Res<T> GetRequiredService<T>() where T : notnull
    {
        return ResC.TWrapR(Services.GetRequiredService<T>,
            $"Failed to retrieve required service of type {typeof(T).FullName}", _logger);
    }
    #endregion

    #region Assembly Loading
    /// <summary>
    /// Loads all relevant services from the assembly into the collection
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="logger"></param>
    private static Res FillCollectionWithAssembly(IServiceCollection collection, ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        logger.Information("Injecting types from Assembly");
        
        var addedCount = 0;
        var loadedTypes = GetLoadableTypesFromAssembly(logger);
        if (!loadedTypes.IsOk) return ResC.Fail(loadedTypes.Msg);

        foreach (var (typeToLoad, attribute) in loadedTypes.Value) 
        {
            AddTypeToCollection(typeToLoad, attribute, collection, logger);
            addedCount++;
        }

        sw.Stop();
        logger.Debug("Injected {addedCount} types in {loadTime}ms", addedCount, sw.ElapsedMilliseconds);
        return ResC.Ok();
    }

    /// <summary>
    /// Filters out types with the correct attribute from the assembly
    /// </summary>
    /// <returns></returns>
    private static Res<List<(Type ImplementationType, LoadIntoDiContainerAttribute Attribute)>> GetLoadableTypesFromAssembly(ILogger logger)
    {
        var compatibleTypes = LaunchUtils.GetCompleteTypesFromAssemblies(logger);
        if (!compatibleTypes.IsOk) 
            return ResC.TFail<List<(Type, LoadIntoDiContainerAttribute)>>(compatibleTypes.Msg);

        List<(Type ImplementationType, LoadIntoDiContainerAttribute Attribute)> loadedTypes = [];
        foreach (var type in compatibleTypes.Value)
        {
            var attribute = type.GetCustomAttribute<LoadIntoDiContainerAttribute>();
            if (attribute != null && IsPlatformCompatible(attribute.SupportedPlatforms))
            {
                loadedTypes.Add((type, attribute));
            }
        }
        return ResC.TOk(loadedTypes);
    }

    /// <summary>
    /// Adds a type to the collection using the implementation type and information from the attribute
    /// </summary>
    private static void AddTypeToCollection(Type implType, LoadIntoDiContainerAttribute attribute, IServiceCollection collection, ILogger logger)
    {
        if (attribute is PrototypeLoadIntoDiContainer prototypeAttribute)
        {
            prototypeAttribute.NotifyAboutLoadedPrototype(implType, logger);
        }

        logger.Debug("Injecting type \"{type}\" as \"{asType}\" to collection with lifetime {lifetime}",
            implType.FullName, attribute.AsType.FullName, attribute.Lifetime.ToString());
        switch (attribute.Lifetime)
        {
            case Lifetime.Transient:
                collection.AddTransient(attribute.AsType, implType);
                break;
            case Lifetime.Scoped:
                collection.AddScoped(attribute.AsType, implType);
                break;
            default:
                collection.AddSingleton(attribute.AsType, implType);
                break;
        }
    }

    /// <summary>
    /// Returns if the current platform is supported by a service
    /// </summary>
    private static bool IsPlatformCompatible(SupportedPlatformFlags flags)
    {
        return flags == SupportedPlatformFlags.All
            || (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && flags.HasFlag(SupportedPlatformFlags.Linux))
            || (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && flags.HasFlag(SupportedPlatformFlags.Windows))
            || (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && flags.HasFlag(SupportedPlatformFlags.OSX));
    }
    #endregion

    #region Dependency Collection
    private record DiServiceInfo(Type Type, IService Impl, List<Type> Deps);
    
    /// <summary>
    /// Collects information from all loaded IServices for establishing a dependency order
    /// </summary>
    /// <returns></returns>
    private Res<List<DiServiceInfo>> CollectDiServiceInfosFromContainer()
    {
        var interfaceService = typeof(IService);
        var interfaceServiceProvider = typeof(IServiceProvider);
        var interfaceBulkLoader = typeof(IContainerBulkLoader<>).GetGenericTypeDefinition();

        var loadedServices = LaunchUtils.GetImplementationsInContainerForClass<IService>(Services, _logger);
        if (!loadedServices.IsOk) return ResC.TFail<List<DiServiceInfo>>(loadedServices.Msg);

        var serviceInfos = new List<DiServiceInfo>();
        List<ResMsg> failMessages = [];
        foreach(var loadedService in loadedServices.Value)
        {
            var serviceType = loadedService.GetType();
            var deps = GetAllServiceDependenciesForType(serviceType, interfaceService,
                interfaceServiceProvider, interfaceBulkLoader, loadedServices.Value);
            
            if (deps.IsOk)
            {
                _logger.Verbose("Assessed {requiredServiceCount} other required IServices for IService \"{serviceType}\"",
                    deps.Value.Count, serviceType.FullName);
                serviceInfos.Add(new(serviceType, loadedService, deps.Value));
            }
            else
            {
                _logger.Warning("Failed assessing required IServices for IService \"{serviceType}\"", serviceType.FullName);
                failMessages.Add(deps.Msg);
            }
        }

        return failMessages.Count == 0 ? ResC.TOk(serviceInfos) : ResC.TFailM<List<DiServiceInfo>>(failMessages);
    }

    /// <summary>
    /// Gets the list of dependencies for an IService
    /// </summary>
    private Res<List<Type>> GetAllServiceDependenciesForType
    (
        Type serviceType,
        Type interfaceService,
        Type interfaceServiceProvider,
        Type interfaceBulkLoader,
        List<IService> availableServices
    )
    {
        var constructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        List<Type> dependencies = [];

        if (constructors.Length == 0)
        {
            return ResC.TFailLog<List<Type>>($"Failed to locate constructor for IService \"{serviceType.FullName}\"", _logger);
        }
        else if (constructors.Length > 1)
        {
            _logger.Warning("Multiple constructors found for IService \"{serviceType}\", using first one", serviceType.FullName);
        }

        foreach(var parameter in constructors[0].GetParameters())
        {
            var parameterType = parameter.ParameterType;
            
            // Service providers are not allowed so we can evaluate bulk dependencies
            if (parameterType.IsAssignableTo(interfaceServiceProvider))
            {
                var message = $"Service \"{serviceType.FullName}\" attempts to inject \"{interfaceServiceProvider.Name}\", please use \"{interfaceBulkLoader.Name}\" instead";
                return ResC.TFailLog<List<Type>>(message, _logger);
            }

            // Handling for bulk dependencies
            if (parameterType.IsConstructedGenericType && interfaceBulkLoader.GUID == parameterType.GetGenericTypeDefinition().GUID)
            {
                var bulkTypes = availableServices
                    .Select(x => x.GetType())
                    .Where(x => x.IsAssignableTo(parameterType.GetGenericArguments()[0]) 
                        && x.IsAssignableTo(interfaceService));
                dependencies.AddRange(bulkTypes);
                continue;
            }

            // Regular IService
            if (parameterType.IsAssignableTo(interfaceService))
            {
                dependencies.Add(parameterType);
            }
        }

        return ResC.TOk(dependencies);
    }
    #endregion

    #region Order Calculation
    private record StartServiceInfo(IAutoStartStopService Service, Type Type, Type AsType);

    /// <summary>
    /// Establishes the order in which services should be started based on dependencies
    /// </summary>
    private Res<List<StartServiceInfo>> EstablishServiceLaunchOrder(List<DiServiceInfo> serviceInfos, bool reversed)
    {
        List<StartServiceInfo> resolvedServices = [];
        List<Type> resolvedServiceTypes = [];

        var resolveLoopCount = 0;
        while (serviceInfos.Count > 0)
        {
            resolveLoopCount++;
            _logger.Verbose("Starting resolve loop {loopCount}, {toResolve} services remain", resolveLoopCount, serviceInfos.Count);
            var res = DoLaunchOrderResolveStep(serviceInfos, resolvedServices, resolvedServiceTypes);
            if (!res.IsOk) return ResC.TFail<List<StartServiceInfo>>(res.Msg);
        }

        if (reversed)
        {
            resolvedServices.Reverse();
        }
        return ResC.TOk(resolvedServices);
    }

    /// <summary>
    /// Represents a single loop for resolving the launch order
    /// </summary>
    private Res DoLaunchOrderResolveStep(List<DiServiceInfo> serviceInfos, List<StartServiceInfo> resolvedServices, List<Type> resolvedServiceTypes)
    {
        bool hasResolvedSomething = false;

        // Reversing through service info list in reverse to allow removal of elements while interating
        for (var i = serviceInfos.Count - 1; i > -1; i--)
        {
            var serviceInfo = serviceInfos[i];

            serviceInfo.Deps.RemoveAll(resolvedServiceTypes.Contains);
            if (serviceInfo.Deps.Count > 0)
                continue;

            var resolvesFor = serviceInfo.Type.GetCustomAttribute<LoadIntoDiContainerAttribute>()?.AsType
                ?? serviceInfo.Type;
            
            if (serviceInfo.Impl is IAutoStartStopService autoStartService)
            {
                var infos = new StartServiceInfo(autoStartService, serviceInfo.Type, resolvesFor);
                resolvedServices.Add(infos);
            }
            resolvedServiceTypes.Add(resolvesFor);
            serviceInfos.RemoveAt(i);

            hasResolvedSomething = true;
            _logger.Verbose("Resolved all dependencies for service \"{resolvedService}\", startup order is {startupOrder}, {toResolve} still resolving",
                serviceInfo.Type.FullName, resolvedServices.Count, serviceInfos.Count);
        }

        if (!hasResolvedSomething)
        {
            var brokenServices = string.Join(", ", serviceInfos.Select(x => x.Type.FullName));
            return ResC.FailLog($"Failed to resolve dependency chain in remaining services ({brokenServices})", _logger, lvl: ResMsgLvl.Fatal);
        }

        return ResC.Ok();
    }
    #endregion

    #region Start / Stop
    /// <summary>
    /// Grabs all services from the container and starts them in an order established using their dependencies
    /// </summary>
    public Res StartServices(Action<string>? onProgress)
    {
        var diagnosticSw = Stopwatch.StartNew();
        _logger.Information("Locating all registered services for startup...");
        onProgress?.Invoke("Locating registered services");

        var serviceResult = CollectDiServiceInfosFromContainer();
        if (!serviceResult.IsOk)
        {
            _logger.Fatal("Failed locating registered services ({result})", serviceResult);
            return ResC.Fail(serviceResult.Msg);
        }
        var registeredServices = serviceResult.Value;

        _logger.Debug("Establishing startup order of {serviceCount} services by resolving dependencies... (DI taken {diDuration}ms so far)",
            registeredServices.Count, diagnosticSw.ElapsedMilliseconds);
        onProgress?.Invoke($"Establishing startup order of {registeredServices.Count} services");
        var orderResult = EstablishServiceLaunchOrder(registeredServices, false);
        if (!orderResult.IsOk)
        {
            _logger.Fatal("Failed establishing startup order ({result})", serviceResult);
            return ResC.Fail(orderResult.Msg);
        }

        var orderedServicesToStart = orderResult.Value;
        _logger.Debug("Order of {toStart} startable services established, proceeding with startup... (DI taken {diDuration}ms so far)",
            orderedServicesToStart.Count, diagnosticSw.ElapsedMilliseconds);
        
        for (var i = 0; i < orderedServicesToStart.Count; i++)
        {
            var currentService = orderedServicesToStart[i];
            _logger.Debug("Starting service {currenStart}/{toStart}: {currentService} as {currentServiceBase}",
                i + 1, orderedServicesToStart.Count, currentService.Type.FullName, currentService.AsType.FullName);
            onProgress?.Invoke($"Starting service {i + 1}/{orderedServicesToStart.Count}:\n{currentService.Type.Name}");

            var subSw = Stopwatch.StartNew();
            var startResult = ResC.Wrap(currentService.Service.Start, $"Failed starting service {currentService.Type.Name}", _logger);
            subSw.Stop();
            if (!startResult.IsOk) //todo: [FEAT] Proper cleanup?
            {
                _logger.Debug("Failed starting service {currenStart}/{toStart}: {currentService} as {currentServiceBase} ({result})",
                    i + 1, orderedServicesToStart.Count, currentService.Type.FullName, currentService.AsType.FullName, startResult);
                return ResC.Fail(startResult.Msg.WithContext($"StartServices > {currentService.Type.Name}"));
            }

            _logger.Debug("Started service {currenStart}/{toStart}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
            i + 1, orderedServicesToStart.Count, currentService.Type.FullName, subSw.ElapsedMilliseconds, diagnosticSw.ElapsedMilliseconds);
        }

        diagnosticSw.Stop();
        _logger.Debug("Successfully started {toStart} startable services in {diDuration}ms", 
            orderedServicesToStart.Count, diagnosticSw.ElapsedMilliseconds);
        onProgress?.Invoke($"Started {orderedServicesToStart.Count} services");
        return ResC.Ok();
    }

    /// <summary>
    /// Grabs all services from the container and stops them in an order established using their dependencies
    /// </summary>
    public Res StopServices()
    {
        var diagnosticSw = Stopwatch.StartNew();
        _logger.Information("Locating all registered services for stopping...");

        var serviceResult = CollectDiServiceInfosFromContainer();
        if (!serviceResult.IsOk)
        {
            _logger.Fatal("Failed locating registered services ({result})", serviceResult);
            return ResC.Fail(serviceResult.Msg);
        }
        var registeredServices = serviceResult.Value;

        _logger.Debug("Establishing reversed startup order of {serviceCount} services by resolving dependencies... (DI taken {diDuration}ms so far)",
            registeredServices.Count, diagnosticSw.ElapsedMilliseconds);
        var orderResult = EstablishServiceLaunchOrder(registeredServices, true);
        if (!orderResult.IsOk)
        {
            _logger.Fatal("Failed establishing startup order of services ({result})", orderResult);
            return ResC.Fail(orderResult.Msg);
        }

        var orderedServicesToStop = orderResult.Value;
        _logger.Debug("Order of {toStop} stoppable services established, proceeding with stopping... (DI taken {diDuration}ms so far)",
            orderedServicesToStop.Count, diagnosticSw.ElapsedMilliseconds);

        List<ResMsg> failedStops = [];
        for (var i = 0; i < orderedServicesToStop.Count; i++)
        {
            var currentService = orderedServicesToStop[i];
            _logger.Debug("Stopping service {currenStart}/{toStop}: {currentService}",
                i + 1, orderedServicesToStop.Count, currentService.Type.FullName);

            var subSw = Stopwatch.StartNew();
            var stopResult = ResC.Wrap(currentService.Service.Stop, $"Failed to stop service {currentService.Type.Name}", _logger);
            subSw.Stop();

            if (!stopResult.IsOk)
            {
                _logger.Error("Failed to stop service {currenStart}/{toStop}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far) ({result})",
                    i + 1, orderedServicesToStop.Count, currentService.Type.FullName, subSw.ElapsedMilliseconds, diagnosticSw.ElapsedMilliseconds, stopResult);
                failedStops.Add(stopResult.Msg);
            }
            else
            {
                _logger.Debug("Stopped service {currenStart}/{toStop}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
                    i + 1, orderedServicesToStop.Count, currentService.Type.FullName, subSw.ElapsedMilliseconds, diagnosticSw.ElapsedMilliseconds);
            }
        }

        diagnosticSw.Stop();
        if (failedStops.Count == 0)
        {
            _logger.Debug("Successfully stopped {toStart} stoppable services in {diDuration}ms",
                orderedServicesToStop.Count, diagnosticSw.ElapsedMilliseconds);
            return ResC.Ok();
        }
        else
        {
            var combinedRes = ResC.FailM(failedStops);
            _logger.Debug("Stopped {toStart} stoppable services in {diDuration}ms with errors ({result})",
                orderedServicesToStop.Count, diagnosticSw.ElapsedMilliseconds, combinedRes);
            return combinedRes;
        }
    }
    #endregion
}