using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling;

/// <summary>
/// Service to handle osc messages
/// </summary>
[LoadIntoDiContainer(typeof(IOscMessageHandlingService), Lifetime.Singleton)]
public class OscMessageHandlingService(ILogger logger, IContainerBulkLoader<IOscMessageHandler> bulkLoader)
    : StartStopServiceBase(logger.ForContext<OscMessageHandlingService>()), IOscMessageHandlingService
{
    private IOscMessageHandler[]? _handlers = null;
    private readonly IContainerBulkLoader<IOscMessageHandler> _bulkLoader = bulkLoader;

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

    #region StartStop
    protected override bool IsStarted()
        => _handlers is not null;
    protected override bool IsProcessing()
        => IsStarted() && _handlers!.Length > 0;

    protected override void StartForService()
    {
        _logger.Verbose("Loading Message Handlers");
        _handlers = _bulkLoader.GetInstances().ToArray();
    }
    protected override bool UseAlreadyStartedProtection => false;
    
    protected override void StopForService()
    {
        _handlers = null;
    }
    #endregion
}