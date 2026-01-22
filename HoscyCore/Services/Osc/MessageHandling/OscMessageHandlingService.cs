using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Interfacing;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling;

/// <summary>
/// Service to handle osc messages
/// </summary>
[LoadIntoDiContainer(typeof(IOscMessageHandlingService), Lifetime.Singleton)]
public class OscMessageHandlingService(ILogger logger, IBackToFrontNotifyService notify, ContainerBulkLoader<IOscMessageHandler> bulkLoader) : StartStopServiceBase, IOscMessageHandlingService
{
    private readonly ILogger _logger = logger.ForContext<OscMessageHandlingService>();
    private readonly IBackToFrontNotifyService _notify = notify; //todo: [FIX] Should this not be implemented?
    private IOscMessageHandler[]? _handlers = null;
    private readonly ContainerBulkLoader<IOscMessageHandler> _bulkLoader = bulkLoader;

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

    //todo: [FEAT] Modules for all handling

    #region StartStop
    protected override bool IsStarted()
        => _handlers is not null;
    protected override bool IsProcessing()
        => IsStarted() && _handlers!.Length > 0;

    public override void Stop()
    {
        _logger.Debug("Clearing module list");
        _handlers = null;
        _logger.Debug("Cleared module list");
    }

    public override void Restart()
    {
        RestartSimple(GetType(), _logger);
    }

    protected override void StartInternal()
    {
        _logger.Debug("Loading Message Handlers");
        _handlers = _bulkLoader.GetInstances().ToArray();
    }
    #endregion
}