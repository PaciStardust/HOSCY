using System.Diagnostics;
using System.Reflection;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCore.Services.DependencyCore;

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
    public static DiContainer CreateWithAssembly(ILogger logger, ConfigModel config, Action<ServiceCollection>? additionalInserts = null)
    {
        var diLogger = logger.ForContext<DiContainer>();
        var sw = Stopwatch.StartNew();
        diLogger.Information("Creating DiContainer with assembly...");

        var collection = new ServiceCollection();
        collection.AddSingleton(diLogger)
            .AddSingleton(config);

        FillCollectionWithAssembly(collection, diLogger);
        additionalInserts?.Invoke(collection);

        return new DiContainer(collection.BuildServiceProvider(), diLogger);
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
    public T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
    #endregion

    #region Assembly Loading
    /// <summary>
    /// Loads all relevant services from the assembly into the collection
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="logger"></param>
    private static void FillCollectionWithAssembly(IServiceCollection collection, ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        logger.Information("Injecting types from Assembly");
        
        var addedCount = 0;
        var loadedTypes = GetLoadableTypesFromAssembly();
        foreach (var (typeToLoad, attribute) in loadedTypes) 
        {
            AddTypeToCollection(typeToLoad, attribute, collection, logger);
            addedCount++;
        }
        sw.Stop();
        logger.Debug("Injected {addedCount} types in {loadTime}ms", addedCount, sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Filters out types with the correct attribute from the assembly
    /// </summary>
    /// <returns></returns>
    private static IEnumerable<(Type ImplementationType, LoadIntoDiContainerAttribute Attribute)> GetLoadableTypesFromAssembly()
    {
        var compatibleTypes = LaunchUtils.GetCompleteTypesFromAssemblies();
        List<(Type ImplementationType, LoadIntoDiContainerAttribute Attribute)> loadedTypes = [];
        foreach (var type in compatibleTypes)
        {
            var attribute = type.GetCustomAttribute<LoadIntoDiContainerAttribute>();
            if (attribute != null)
            {
                loadedTypes.Add((type, attribute));
            }
        }
        return loadedTypes;
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
    #endregion

    #region Dependency Collection
    private record DiServiceInfo(Type Type, IService Impl, List<Type> Deps);
    
    /// <summary>
    /// Collects information from all loaded IServices for establishing a dependency order
    /// </summary>
    /// <returns></returns>
    private List<DiServiceInfo> CollectDiServiceInfosFromContainer()
    {
        var interfaceService = typeof(IService);
        var interfaceServiceProvider = typeof(IServiceProvider);
        var interfaceBulkLoader = typeof(IContainerBulkLoader<>).GetGenericTypeDefinition();

        var loadedServices = LaunchUtils.GetImplementationsInContainerForClass<IService>(Services, _logger);

        var serviceInfos = new List<DiServiceInfo>();
        foreach(var loadedService in loadedServices)
        {
            var serviceType = loadedService.GetType();
            var deps = GetAllServiceDependenciesForType(serviceType, interfaceService, interfaceServiceProvider, interfaceBulkLoader, loadedServices);
            
            _logger.Verbose("Assessed {requiredServiceCount} other required IServices for IService \"{serviceType}\"",
                deps.Count, serviceType.FullName);

            serviceInfos.Add(new(serviceType, loadedService, deps));
        }
        return serviceInfos;
    }

    /// <summary>
    /// Gets the list of dependencies for an IService
    /// </summary>
    private List<Type> GetAllServiceDependenciesForType(Type serviceType, Type interfaceService, Type interfaceServiceProvider, Type interfaceBulkLoader, List<IService> availableServices)
    {
        var constructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        List<Type> dependencies = [];

        if (constructors.Length == 0)
        {
            _logger.Warning("Failed to locate constructor for IService \"{serviceType}\"", serviceType.FullName);
            return dependencies;
        } else if (constructors.Length > 1)
        {
            _logger.Warning("Multiple constructors found for IService \"{serviceType}\", using first one", serviceType.FullName);
        }

        foreach(var parameter in constructors[0].GetParameters())
        {
            var parameterType = parameter.ParameterType;
            
            // Service providers are not allowed so we can evaluate bulk dependencies
            if (parameterType.IsAssignableTo(interfaceServiceProvider))
            {
                _logger.Error("Service \"{service}\" attempts to inject \"{serviceCollection}\", please use \"{bulk}\" instead",
                serviceType.FullName, interfaceServiceProvider.Name, interfaceBulkLoader.Name);
                throw new DiResolveException($"Service \"{serviceType.FullName}\" attempts to inject \"{interfaceServiceProvider.Name}\", please use \"{interfaceBulkLoader.Name}\" instead");
            }

            // Handling for bulk dependencies
            if (parameterType.IsConstructedGenericType && interfaceBulkLoader.GUID == parameterType.GetGenericTypeDefinition().GUID)
            {
                var bulkTypes = availableServices
                    .Select(x => x.GetType())
                    .Where(x => x.IsAssignableTo(parameterType.GetGenericArguments()[0]));
                dependencies.AddRange(bulkTypes);
                continue;
            }

            // Regular IService
            if (parameterType.IsAssignableTo(interfaceService))
            {
                dependencies.Add(parameterType);
            }
        }

        return dependencies;
    }
    #endregion

    #region Order Calculation
    private record StartServiceInfo(IAutoStartStopService Service, Type Type, Type AsType);

    /// <summary>
    /// Establishes the order in which services should be started based on dependencies
    /// </summary>
    private List<StartServiceInfo> EstablishServiceLaunchOrder(List<DiServiceInfo> serviceInfos, bool reversed)
    {
        List<StartServiceInfo> resolvedServices = [];
        List<Type> resolvedServiceTypes = [];

        var resolveLoopCount = 0;
        while (serviceInfos.Count > 0)
        {
            resolveLoopCount++;
            _logger.Verbose("Starting resolve loop {loopCount}, {toResolve} services remain", resolveLoopCount, serviceInfos.Count);
            DoLaunchOrderResolveStep(serviceInfos, resolvedServices, resolvedServiceTypes);
        }

        if (reversed)
        {
            resolvedServices.Reverse();
        }
        return resolvedServices;
    }

    /// <summary>
    /// Represents a single loop for resolving the launch order
    /// </summary>
    private void DoLaunchOrderResolveStep(List<DiServiceInfo> serviceInfos, List<StartServiceInfo> resolvedServices, List<Type> resolvedServiceTypes)
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
            var ex = new DiResolveException($"Failed to resolve dependency chain in remaining services ({brokenServices})");
            _logger.Fatal(ex, "Unable to resolve dependency chain for the following services: \"{brokenServices}\"", brokenServices);
            throw ex;
        }
    }
    #endregion

    #region Start / Stop
    /// <summary>
    /// Grabs all services from the container and starts them in an order established using their dependencies
    /// </summary>
    public void StartServices(Action<string>? onProgress)
    {
        var diagnosticSw = Stopwatch.StartNew();
        _logger.Information("Locating all registered services for startup...");
        onProgress?.Invoke("Locating registered services");
        var registeredServices = CollectDiServiceInfosFromContainer();

        _logger.Debug("Establishing startup order of {serviceCount} services by resolving dependencies... (DI taken {diDuration}ms so far)",
            registeredServices.Count, diagnosticSw.ElapsedMilliseconds);
        onProgress?.Invoke($"Establishing startup order of {registeredServices.Count} services");

        var orderedServicesToStart = EstablishServiceLaunchOrder(registeredServices, false);

        _logger.Debug("Order of {toStart} startable services established, proceeding with startup... (DI taken {diDuration}ms so far)",
            orderedServicesToStart.Count, diagnosticSw.ElapsedMilliseconds);
        
        for (var i = 0; i < orderedServicesToStart.Count; i++)
        {
            var currentService = orderedServicesToStart[i];

            _logger.Debug("Starting service {currenStart}/{toStart}: {currentService} as {currentServiceBase}",
                i + 1, orderedServicesToStart.Count, currentService.Type.FullName, currentService.AsType.FullName);
            onProgress?.Invoke($"Starting service {i + 1}/{orderedServicesToStart.Count}:\n{currentService.GetType().Name}");

            var subSw = Stopwatch.StartNew();
            currentService.Service.Start();
            subSw.Stop();

            _logger.Debug("Started service {currenStart}/{toStart}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
            i + 1, orderedServicesToStart.Count, currentService.Type.FullName, subSw.ElapsedMilliseconds, diagnosticSw.ElapsedMilliseconds);
        }

        diagnosticSw.Stop();
        _logger.Debug("Successfully started {toStart} startable services in {diDuration}ms", 
            orderedServicesToStart.Count, diagnosticSw.ElapsedMilliseconds);
        onProgress?.Invoke($"Started {orderedServicesToStart.Count} services");
    }

    /// <summary>
    /// Grabs all services from the container and stops them in an order established using their dependencies
    /// </summary>
    public void StopServices()
    {
        var diagnosticSw = Stopwatch.StartNew();
        _logger.Information("Locating all registered services for stopping...");
        var registeredServices = CollectDiServiceInfosFromContainer();

        _logger.Debug("Establishing reversed startup order of {serviceCount} services by resolving dependencies... (DI taken {diDuration}ms so far)",
            registeredServices.Count, diagnosticSw.ElapsedMilliseconds);
        var orderedServicesToStop = EstablishServiceLaunchOrder(registeredServices, true);

        _logger.Debug("Order of {toStop} stoppable services established, proceeding with stopping... (DI taken {diDuration}ms so far)",
            orderedServicesToStop.Count, diagnosticSw.ElapsedMilliseconds);

        for (var i = 0; i < orderedServicesToStop.Count; i++)
        {
            var currentService = orderedServicesToStop[i];

            _logger.Debug("Stopping service {currenStart}/{toStop}: {currentService}",
                i + 1, orderedServicesToStop.Count, currentService.Type.FullName);

            var subSw = Stopwatch.StartNew();
            try
            {
                currentService.Service.Stop();
                subSw.Stop();

                _logger.Debug("Stopped service {currenStart}/{toStop}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
                    i + 1, orderedServicesToStop.Count, currentService.Type.FullName, subSw.ElapsedMilliseconds, diagnosticSw.ElapsedMilliseconds);
            } catch (Exception ex)
            {
                subSw.Stop();

                _logger.Error(ex, "Failed to stop service {currenStart}/{toStop}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
                    i + 1, orderedServicesToStop.Count, currentService.Type.FullName, subSw.ElapsedMilliseconds, diagnosticSw.ElapsedMilliseconds);
            }
        }

        diagnosticSw.Stop();
        _logger.Debug("Successfully stopped {toStart} stoppable services in {diDuration}ms", orderedServicesToStop.Count, diagnosticSw.ElapsedMilliseconds);
    }
    #endregion
}