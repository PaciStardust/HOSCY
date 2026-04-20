using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
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

    protected override Res StartForService()
    {
        _logger.Verbose("Loading Message Handlers");
        _handlers = null;

        var res = _bulkLoader.GetInstances();
        if (!res.IsOk)
            return ResC.Fail(res.Msg);

        _handlers = [];
        if (res.Value.Count == 0)
        {
            var msg = ResMsg.Wrn("No message handlers could be located, service will have no functionality and will be NOT be marked as running");
            SetFaultLogNotify(msg, title: "Failed to load Handlers", null, _logger);
            return ResC.Ok();
        }

        _handlers = res.Value.ToArray();
        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => false;
    
    protected override Res StopForService()
    {
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _handlers = null;
    }
    #endregion
}