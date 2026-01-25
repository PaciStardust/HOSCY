using System.Diagnostics;
using System.Reflection;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCore.Services.DependencyCore;

/// <summary>
/// Container for resolving dependencies
/// </summary>
public class DiContainer
{
    public ServiceProvider Services { get; init; }
    private readonly ILogger _internalLogger;

    private DiContainer(ServiceProvider provider, ILogger internalLogger)
    {
        Services = provider;
        _internalLogger = internalLogger;
    }

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

    /// <summary>
    /// Creates a DiContainer using a Logger, ConfigModel, some manual extra additions and all classes with the loader attribute
    /// </summary>
    /// <param name="logger">Logger for container</param>
    /// <param name="config">ConfigModel for container</param>
    /// <param name="additionalInserts">An action to insert additional dependencies manually</param>
    /// <returns>DiContainer with all dependencies loaded in</returns>
    public static DiContainer LoadFromAssembly(ILogger logger, ConfigModel config, Action<ServiceCollection>? additionalInserts = null)
    {
        var internalLogger = logger.ForContext<DiContainer>();
        var sw = Stopwatch.StartNew();
        internalLogger.Information("Creating DI container...");

        var collection = new ServiceCollection();
        collection.AddSingleton(logger)
            .AddSingleton(config);

        AddFromAssembly(collection, internalLogger);
        additionalInserts?.Invoke(collection);

        return new DiContainer(collection.BuildServiceProvider(), internalLogger);
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
    /// Loads services with attribute into IServiceCollection
    /// </summary>
    private static void AddFromAssembly(IServiceCollection collection, ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        logger.Information("Loading dependencies from Assembly");
        var addedCount = 0;
        var allServices = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Distinct();

        foreach (var service in allServices) 
        {
            if (TryAddTypeToCollection(collection, service, logger))
            {
                addedCount++;
            }
        }
        sw.Stop();
        logger.Debug("Loaded {addedCount} dependencies in {loadTime}ms", addedCount, sw.ElapsedMilliseconds);
    }

    private static bool TryAddTypeToCollection(IServiceCollection collection, Type service, ILogger logger)
    {
        if (service.IsAbstract || service.IsInterface) return false;

        var containerAttribute = service.GetCustomAttribute<LoadIntoDiContainerAttribute>();
        if (containerAttribute is null)
        {
            return false;
        }
        else if (containerAttribute is PrototypeLoadIntoDiContainer prototypeAttribute)
        {
            prototypeAttribute.NotifyAboutLoadedPrototype(service, logger);
        }

        logger.Debug("Adding \"{service}\" to DI container with lifetime {lifetime}", service.FullName, containerAttribute.Lifetime.ToString());

        switch (containerAttribute.Lifetime)
        {
            case Lifetime.Transient:
                collection.AddTransient(containerAttribute.AsType, service);
                break;
            case Lifetime.Scoped:
                collection.AddScoped(containerAttribute.AsType, service);
                break;
            default:
                collection.AddSingleton(containerAttribute.AsType, service);
                break;
        }
        return true;
    }

    /// <summary>
    /// Grabs all Services from the container and starts them in an order established using their dependencies
    /// </summary>
    public void StartServices(Action<string>? onProgress)
    {
        var sw = Stopwatch.StartNew();
        _internalLogger.Information("Locating all IServices for startup...");
        onProgress?.Invoke("Locating services to start");
        var servicesToStart = RetrieveServiceInfos();

        _internalLogger.Debug("Establishing startup order of {serviceCount} IServices by resolving dependencies... (DI taken {diDuration}ms so far)",
            servicesToStart.Count, sw.ElapsedMilliseconds);
        onProgress?.Invoke($"Establishing startup order of {servicesToStart.Count} services");
        var (servicesResolvedInOrder, servicesInOrder) = EstablishStartOrder(servicesToStart);

        _internalLogger.Debug("Order of {toStart} IAutoStartStopServices established, proceeding with startup... (DI taken {diDuration}ms so far)",
            servicesInOrder.Count, sw.ElapsedMilliseconds);
        for (var i = 0; i < servicesInOrder.Count; i++)
        {
            var currentService = servicesInOrder[i];
            _internalLogger.Debug("Starting IAutoStartStopServices {currenStart}/{toStart}: {currentService} as {currentServiceBase}",
            i + 1, servicesInOrder.Count, currentService.GetType().Name, servicesResolvedInOrder[i].FullName);
            onProgress?.Invoke($"Starting service {i + 1}/{servicesInOrder.Count}:\n{currentService.GetType().Name}");
            var subSw = Stopwatch.StartNew();
            currentService.Start();
            subSw.Stop();
            _internalLogger.Debug("Started IAutoStartStopServices {currenStart}/{toStart}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
            i + 1, servicesInOrder.Count, servicesResolvedInOrder[i].FullName, subSw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
        }
        sw.Stop();
        _internalLogger.Debug("Successfully started {toStart} IAutoStartStopServices in {diDuration}ms", servicesInOrder.Count, sw.ElapsedMilliseconds);
        onProgress?.Invoke($"Started {servicesInOrder.Count} services");
    }

    /// <summary>
    /// Grabs all Services from the container and stops them in an order established using their dependencies
    /// </summary>
    public void StopServices()
    {
        var sw = Stopwatch.StartNew();
        _internalLogger.Information("Locating all IServices for stopping...");
        var servicesToStop = RetrieveServiceInfos();

        _internalLogger.Debug("Establishing reversed startup order of {serviceCount} IServices by resolving dependencies... (DI taken {diDuration}ms so far)",
            servicesToStop.Count, sw.ElapsedMilliseconds);
        var (servicesResolvedInOrder, servicesInOrder) = EstablishStartOrder(servicesToStop, true);

        _internalLogger.Debug("Order of {toStop} IAutoStartStopServices established, proceeding with stopping... (DI taken {diDuration}ms so far)",
            servicesInOrder.Count, sw.ElapsedMilliseconds);
        for (var i = 0; i < servicesInOrder.Count; i++)
        {
            var subSw = Stopwatch.StartNew();
            var currentService = servicesInOrder[i];
            _internalLogger.Debug("Stopping IAutoStartStopServices {currenStart}/{toStop}: {currentService}",
            i + 1, servicesInOrder.Count, servicesResolvedInOrder[i].FullName);
            try
            {
                currentService.Stop();
                subSw.Stop();
                _internalLogger.Debug("Stopped IAutoStartStopServices {currenStart}/{toStop}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
            i + 1, servicesInOrder.Count, servicesResolvedInOrder[i].FullName, subSw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
            } catch (Exception ex)
            {
                subSw.Stop();
                _internalLogger.Error(ex, "Failed to stop IAutoStartStopServices {currenStart}/{toStop}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
                i + 1, servicesInOrder.Count, servicesResolvedInOrder[i].FullName, subSw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
            }
        }

        sw.Stop();
        _internalLogger.Debug("Successfully stopped {toStart} IAutoStartStopServices in {diDuration}ms", servicesInOrder.Count, sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Retrieves instance and list of otherr Services in constructor if applicable to Type
    /// </summary>
    /// <returns>Null if Type is not Service or no Instance is available</returns>
    private List<(Type Type, IService Implementation, List<Type> Dependencies)> RetrieveServiceInfos()
    {
        var serviceImplementations = LaunchUtils.GetImplementationsInContainerForClass<IService>(Services, _internalLogger);
        var serviceInterface = typeof(IService);
        var serviceProviderType = typeof(IServiceProvider);
        var bulkLoaderNoGenericType = typeof(ContainerBulkLoader<>).GetGenericTypeDefinition();

        List<(Type Type, IService Implementation, List<Type> Dependencies)> locatedServices = [];

        foreach (var impl in serviceImplementations)
        {
            var type = impl.GetType();
            var ctors = type.GetConstructors();
            if (ctors.Length == 0)
            {
                _internalLogger.Warning("Failed to locate constructor for IService \"{serviceType}\"", type.FullName);
                continue;
            }
            else if (ctors.Length > 0)
            {
                _internalLogger.Debug("Located multiple constructors for IService \"{serviceType}\", picking first", type.FullName);
            }

            List<Type> requiredServiceTypes = [];
            foreach(var parameter in ctors[0].GetParameters())
            {
                var parameterType = parameter.ParameterType;

                if (parameterType.IsAssignableTo(serviceProviderType))
                {
                    _internalLogger.Error("Service \"{service}\" attempts to inject \"{serviceCollection}\", please use \"{bulk}\" instead",
                    type.FullName, serviceProviderType.Name, bulkLoaderNoGenericType.Name);
                    throw new DiResolveException($"Service \"{type.FullName}\" attempts to inject \"{serviceProviderType.Name}\", please use \"{bulkLoaderNoGenericType.Name}\" instead");
                }

                if (parameterType.IsConstructedGenericType && bulkLoaderNoGenericType.GUID == parameterType.GetGenericTypeDefinition().GUID)
                {
                    var bulkTypes = serviceImplementations
                        .Select(x => x.GetType())
                        .Where(x => x.IsAssignableTo(parameterType.GetGenericArguments()[0]));
                    requiredServiceTypes.AddRange(bulkTypes);
                    continue;
                }

                if (parameterType.IsAssignableTo(serviceInterface))
                {
                    requiredServiceTypes.Add(parameterType);
                }
            }
            _internalLogger.Debug("Assessed {requiredServiceCount} other required IServices for IService \"{serviceType}\"",
                requiredServiceTypes.Count, type.FullName);

            locatedServices.Add((type, impl, requiredServiceTypes));
        }
        return locatedServices;
    }

    /// <summary>
    /// Establishing startup order of IAutoStartStopServices by resolving dependencies
    /// </summary>
    /// <returns>A list of types and instances in correct order</returns>
    private (List<Type> ResolvedOrdered, List<IAutoStartStopService> ToStartOrdered) EstablishStartOrder(List<(Type Type, IService Implementation, List<Type> Dependencies)> serviceList, bool reversed = false)
    {
        List<Type> servicesResolvedInOrder = [];
        List<IAutoStartStopService> servicesInOrder = [];
        var resolveLoops = 0;
        while (serviceList.Count > 0)
        {
            resolveLoops++;
            _internalLogger.Verbose("Starting resolve loop {loopCount}, {toResolve} IServices remain", resolveLoops, serviceList.Count);
            bool changesMade = false;
            for (var i = serviceList.Count - 1; i > -1; i--)
            {
                var (serviceType, serviceImplementation, serviceDependencies) = serviceList[i];
                var requiredServiceList = serviceDependencies;
                requiredServiceList.RemoveAll(servicesResolvedInOrder.Contains);
                if (requiredServiceList.Count == 0)
                {
                    serviceList.RemoveAt(i);
                    changesMade = true;
                    var resolvesFor = serviceType.GetCustomAttribute<LoadIntoDiContainerAttribute>()?.AsType ?? serviceType;
                    servicesResolvedInOrder.Add(resolvesFor);

                    if (serviceImplementation is IAutoStartStopService autoStartStopService)
                    {
                        servicesInOrder.Add(autoStartStopService);
                        _internalLogger.Debug("Resolved all dependencies for IAutoStartStopService \"{resolvedService}\", startup order is {startupOrder}, {toResolve} still resolving",
                        serviceType.FullName, servicesInOrder.Count, serviceList.Count);
                    } else
                    {
                        _internalLogger.Debug("Resolved all dependencies for IService \"{resolvedService}\", no startup needed as not IAutoStartService, {toResolve} still resolving",
                        serviceType.FullName, servicesInOrder.Count, serviceList.Count);
                    }
                }
            }
            if (!changesMade)
            {
                var brokenServices = string.Join(", ", serviceList.Select(x => x.Type.FullName));
                var ex = new DiResolveException($"Failed to resolve dependency chain in remaining StartStopServices ({brokenServices})");
                _internalLogger.Fatal(ex, "Unable to resolve dependency chain for the following StartStopServices: \"{brokenServices}\"", brokenServices);
                throw ex;
            }
        }

        if (reversed)
        {
            servicesResolvedInOrder.Reverse();
            servicesInOrder.Reverse();
        }
        return (servicesResolvedInOrder, servicesInOrder);
    }
}