using System;
using System.Collections.Generic;
using System.Reflection;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Hoscy.Utility;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc.MessageHandling;

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

    public override void Restart()
        => RestartSimple(GetType().Name, _logger);

    protected override void StartInternal()
    {
        _logger.Information("Loading Message Handlers");
        _handlers = LaunchUtils.GetImplementationsInContainerForClass<IOscMessageHandler>(_services, _logger);
    }
    #endregion
}