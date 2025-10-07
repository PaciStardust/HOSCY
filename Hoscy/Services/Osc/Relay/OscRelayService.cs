using System;
using System.Collections.Generic;
using System.Linq;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using LucHeart.CoreOSC;
using Serilog;

namespace Hoscy.Services.Osc.Relay;

[LoadIntoDiContainer(typeof(IOscRelayService), Lifetime.Singleton)]
public class OscRelayService(ILogger logger, ConfigModel config, IOscSendService sender, IBackToFrontNotifyService notify) : StartStopServiceBase, IOscRelayService
{
    private readonly ILogger _logger = logger.ForContext<OscRelayService>();
    private readonly ConfigModel _config = config;
    private readonly IOscSendService _sender = sender;
    private readonly IBackToFrontNotifyService _notify = notify;

    public bool HasInvalidFilters { get; private set; } = false;
    private List<OscReadonlyRelayFilter> _filters = [];

    #region Start / Stop 
    protected override void StartInternal()
    {
        _logger.Information("Starting Relay Service...");
        ReloadValidRelayFilters(_config.Osc_Relay_Filters.ToList());
        _logger.Information("Started Relay Service");
    }

    public override void Stop()
    {
        _logger.Information("Stopping Relay Service...");
        ReloadValidRelayFilters([]);
        _logger.Information("Stopped Relay Service...");
    }

    public override bool TryRestart()
    {
        try
        {
            _logger.Information("Restarting Relay Service...");
            ReloadValidRelayFilters(_config.Osc_Relay_Filters.ToList());
            _logger.Information("Restarted Relay Service");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to restart Relay Filters");
            _notify.SendError("Failed to restart Relay Filters", exception: ex);
            return false;
        }
    }

    public override bool IsRunning()
    {
        return true;
    }
    #endregion

    #region Functionality
    public void HandleRelay(OscMessage message)
    {
        foreach (var filter in _filters)
        {
            if (!filter.Matches(message.Address)) continue;
            _sender.SendSyncFireAndForget(filter.Ip, filter.Port, message.Address, message.Arguments); //todo: should this be fire and forget?
        }
    }

    private void ReloadValidRelayFilters(List<OscRelayFilterModel> filterModels)
    {
        const string OSC_TEST_ADDRESS = "/osctest123";

        logger.Information("Loading relay filters...");
        HasInvalidFilters = false;
        _filters.Clear();
        List<OscReadonlyRelayFilter> filters = [];
        if (filterModels.Count == 0)
        {
            logger.Information("No relay filters found");
            return;
        }

        foreach (var filterModel in filterModels)
        {
            var readonlyFilter = new OscReadonlyRelayFilter(filterModel);
            logger.Debug("Checking validity of Relay Filter {filterName}", readonlyFilter.Name);

            var result = !string.IsNullOrWhiteSpace(readonlyFilter.Ip)
                && !string.IsNullOrWhiteSpace(readonlyFilter.Name)
                && readonlyFilter.Port != ushort.MinValue
                && _sender.SendSync(readonlyFilter.Ip, readonlyFilter.Port, OSC_TEST_ADDRESS, false);
            if (!result)
            {
                _logger.Warning("Skipping creation of listener \"{filterName}\" as its values are invalid (Name / Port / Ip / Filters)", readonlyFilter.Name);
                filterModel.SetValidity(false);
                HasInvalidFilters = true;
                continue;
            }

            filterModel.SetValidity(true);
            filters.Add(readonlyFilter);
            logger.Debug("Relay Filter {filterName} is valid and has been added to list", readonlyFilter.Name);
        }

        _filters = filters;
        logger.Information("{currentFilterCount}/{allFiltersCount} filters have been loaded", filters.Count, filterModels.Count);
    }
    #endregion
}