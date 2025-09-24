using System;
using System.Diagnostics;
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
}