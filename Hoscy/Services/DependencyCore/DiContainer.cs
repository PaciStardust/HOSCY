using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Hoscy.Configuration.Modern;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;

namespace Hoscy.Services.DependencyCore;

public class DiContainer
{
    public ServiceProvider Services { get; init; }
    private readonly ILogger _internalLogger;

    private DiContainer(ServiceProvider provider, ILogger internalLogger)
    {
        Services = provider;
        _internalLogger = internalLogger;
    }

    public static DiContainer LoadFromAssembly(Logger logger, ConfigModel config, Action<ServiceCollection>? additionalInserts = null)
    {
        var internalLogger = logger.ForContext<DiContainer>();
        var sw = Stopwatch.StartNew();
        internalLogger.Information("Creating DI container...");

        var collection = new ServiceCollection();
        collection.AddSingleton(logger)
            .AddSingleton(config);

        AddFromAssembly(collection, internalLogger);
        additionalInserts?.Invoke(collection);

        //todo: actual loading, proper logging

        return new DiContainer(collection.BuildServiceProvider(), internalLogger);
    }

    private static void AddFromAssembly(IServiceCollection collection, ILogger logger)
    {
        var sw = Stopwatch.StartNew();
        logger.Debug("Loading dependencies from Assembly");
        var addedCount = 0;
        foreach (var service in Assembly.GetExecutingAssembly().GetTypes())
        {
            var containerAttribute = service.GetCustomAttribute<LoadIntoDiContainerAttribute>();
            if (containerAttribute is null)
                continue;

            logger.Debug("Adding {service} to DI container with lifetime {lifetime}", service.FullName, containerAttribute.Lifetime.ToString());

            switch (containerAttribute.Lifetime)
            {
                case Lifetime.Transient: collection.AddTransient(service); break;
                case Lifetime.Scoped: collection.AddScoped(service); break;
                default: collection.AddSingleton(service); break;
            }
            addedCount++;
        }
        sw.Stop();
        logger.Debug("Loaded {addedCount} dependencies in {loadTime}ms", addedCount, sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Grabs all Services from the container and starts them in an order established using their dependencies
    /// </summary>
    public void StartServices()
    {
        _internalLogger.Information("Locating all StartStopServices for startup...");
        var sw = Stopwatch.StartNew();

        List<(Type, IStartStopService, List<Type>)> servicesToStart = [];
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!RetrieveStartStopServiceInfosForType(type, out var instance, out var requiredServiceTypes)) continue;

            _internalLogger.Debug("Assessed {requiredServiceCount} other required StartStopServices for StartStopService {serviceType}",
            requiredServiceTypes!.Count, type.FullName);
            servicesToStart.Add((type, instance!, requiredServiceTypes));
        }

        _internalLogger.Information("Establishing startup order of {serviceCount} StartStopServices by resolving dependencies... (DI taken {diDuration}ms so far)",
            servicesToStart.Count, sw.ElapsedMilliseconds);
        var  (servicesResolvedInOrder, servicesInOrder) = EstablishStartOrder(servicesToStart);

        _internalLogger.Information("Order of {toStart} StartStopServices established, proceeding with startup... (DI taken {diDuration}ms so far)",
            servicesInOrder.Count, sw.ElapsedMilliseconds);
        for (var i = 0; i < servicesInOrder.Count; i++)
        {
            var subSw = Stopwatch.StartNew();
            var currentService = servicesInOrder[i];
            _internalLogger.Debug("Starting StartStopServices {currenStart}/{toStart}: {currentService}",
            i + 1, servicesInOrder.Count, servicesResolvedInOrder[i].FullName);
            currentService.Start();
            subSw.Stop();
            _internalLogger.Debug("Started StartStopServices {currenStart}/{toStart}: {currentService} (Took {startDuration}ms, DI taken {diDuration}ms so far)",
            i + 1, servicesInOrder.Count, servicesResolvedInOrder[i].FullName, subSw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
        }

        sw.Stop();
        _internalLogger.Information("Successfully started {toStart} StartStopServices in {diDuration}ms", servicesInOrder.Count, sw.ElapsedMilliseconds);
    }

    public void StopServices()
    {
        //todo: this
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves instance and list of other StartStopServices in constructor if applicable to Type
    /// </summary>
    /// <returns>Null if Type is not StartStopService or no Instance is available</returns>
    private bool RetrieveStartStopServiceInfosForType(Type type, out IStartStopService? serviceInstance, out List<Type>? ctorTypes)
    {
        var startstopServiceInterface = typeof(IStartStopService);
        serviceInstance = null;
        ctorTypes = null;

        if (type.IsInterface || !type.IsAssignableTo(startstopServiceInterface)) return false;

        if (Services.GetService(type) is not IStartStopService instance)
        {
            _internalLogger.Debug("Could not locate instance of StartStopService {serviceType}", type.FullName);
            return false;
        }
        _internalLogger.Debug("Located instance of StartStopService {serviceType}", type.FullName);

        var ctors = type.GetConstructors();
        if (ctors.Length == 0)
        {
            _internalLogger.Warning("Failed to locate constructor for StartStopService {serviceType}", type.FullName);
            return false;
        }
        else if (ctors.Length > 0)
        {
            _internalLogger.Debug("Located multiple constructors for StartStopService {serviceType}, picking first", type.FullName);
        }

        ctorTypes = ctors[0].GetParameters()
            .Select(x => x.ParameterType)
            .Where(x => !x.IsInterface && x.IsAssignableTo(startstopServiceInterface))
            .ToList();
        serviceInstance = instance;
        return true;
    }

    public (List<Type>, List<IStartStopService>) EstablishStartOrder(List<(Type, IStartStopService, List<Type>)> servicesToStart)
    {
        List<Type> servicesResolvedInOrder = [];
        List<IStartStopService> servicesInOrder = [];
        var resolveLoops = 0;
        while (servicesToStart.Count > 0)
        {
            resolveLoops++;
            _internalLogger.Verbose("Starting resolve loop {loopCount}, {toResolve} StartStopServices remain", resolveLoops, servicesToStart.Count);
            bool changesMade = false;
            for (var i = servicesToStart.Count - 1; i > -1; i--)
            {
                var serviceInfo = servicesToStart[i];
                var requiredServiceList = serviceInfo.Item3;
                requiredServiceList.RemoveAll(servicesResolvedInOrder.Contains);
                if (requiredServiceList.Count == 0)
                {
                    servicesResolvedInOrder.Add(serviceInfo.Item1);
                    servicesInOrder.Add(serviceInfo.Item2);
                    servicesToStart.RemoveAt(i);
                    _internalLogger.Debug("Resolved all dependencies for StartStopService {resolvedService}, startup order is {startupOrder}, {toResolve} still resolving",
                        serviceInfo.Item1.FullName, servicesInOrder.Count, servicesToStart.Count);
                    changesMade = true;
                }
            }
            if (!changesMade)
            {
                var brokenServices = string.Join(", ", servicesToStart.Select(x => x.Item1.FullName));
                var ex = new DiResolveException($"Failed to resolve dependency chain in remaining StartStopServices ({brokenServices})");
                _internalLogger.Fatal(ex, "Unable to resolve dependency chain for the following StartStopServices: {brokenServices}", brokenServices);
                throw ex;
            }
        }
        return (servicesResolvedInOrder, servicesInOrder);
    }
}