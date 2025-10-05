using System;
using System.Collections.Generic;
using System.Reflection;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc;

/// <summary>
/// Service to handle osc messages
/// </summary>
[LoadIntoDiContainer(typeof(IOscMessageHandlingService), Lifetime.Singleton)]
public class OscMessageHandlingService(ILogger logger, IBackToFrontNotifyService notify, IServiceProvider serviceProvider) : StartStopServiceBase, IOscMessageHandlingService
{
    private readonly ILogger _logger = logger.ForContext<OscMessageHandlingService>();
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly IServiceProvider _services = serviceProvider;
    private List<IOscMessageHandler>? _handlers = null;

    /// <summary>
    /// Sends message to all message handlers
    /// </summary>
    /// <returns>True if handled</returns>
    public bool HandleMessage(OscMessage message)
    {
        if (_handlers is null) return false;
        foreach (var module in _handlers)
        {
            var ret = module.HandleMessage(message);
            if (ret) return true;
        }
        return false;
    }

    //todo: modules for all handling

    #region StartStop
    public override bool IsRunning()
    {
        return _handlers is not null;
    }

    public override void Stop()
    {
        _logger.Information("Clearing module list");
        _handlers = null;
        _logger.Information("Cleared module list");
    }

    public override bool TryRestart()
        => TryRestartSimple(GetType().Name, _logger, _notify);

    protected override void StartInternal()
    {
        _logger.Information("Loading Message Handlers");
        var controlModuleInterface = typeof(IOscMessageHandler);
        List<IOscMessageHandler> handlers = [];
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.IsInterface || type.IsAbstract || !type.IsAssignableTo(controlModuleInterface)) continue;

            var diType = type.GetCustomAttribute<LoadIntoDiContainerAttribute>()?.AsType ?? type;

            if (_services.GetService(diType) is not IOscMessageHandler instance)
            {
                _logger.Debug("Could not locate instance of Message Handler {serviceType}", type.FullName);
                continue;
            }
            _logger.Debug("Located instance of Message Handler {serviceType}", type.FullName);
            handlers.Add(instance);
        }
        _logger.Information("Loaded {mmoduleCount} Message Handlers", handlers.Count);
        _handlers = handlers;
    }
    #endregion
}