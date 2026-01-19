using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Osc.SendReceive;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.Relay;

[LoadIntoDiContainer(typeof(IOscRelayService), Lifetime.Singleton)]
public class OscRelayService(ILogger logger, ConfigModel config, IOscSendService sender, IBackToFrontNotifyService notify) : StartStopServiceBase, IOscRelayService //todo: [TEST] Does relay works?
{
    private readonly ILogger _logger = logger.ForContext<OscRelayService>();
    private readonly ConfigModel _config = config;
    private readonly IOscSendService _sender = sender;
    private readonly IBackToFrontNotifyService _notify = notify;

    private List<OscReadonlyRelayFilter>? _filters = [];

    #region Start / Stop 
    protected override void StartInternal()
    {
        LogStartBegin(GetType(), _logger);
        ReloadValidRelayFilters(_config.Osc_Relay_Filters.ToList());
        LogStartComplete(GetType(), _logger);
    }

    public override void Stop()
    {
        LogStopBegin(GetType(), _logger);
        ReloadValidRelayFilters([]);
        _filters = null;
        LogStopComplete(GetType(), _logger);
    }

    public override void Restart()
    {
        LogRestartBegin(GetType(), _logger);
        ReloadValidRelayFilters(_config.Osc_Relay_Filters.ToList());
        LogRestartComplete(GetType(), _logger);
    }

    protected override bool IsStarted()
        => _filters is not null;
    protected override bool IsProcessing()
        => IsStarted() && _filters!.Count > 0;
    #endregion

    #region Functionality
    public void HandleRelay(OscMessage message)
    {
        if (_filters is null) return;
        foreach (var filter in _filters)
        {
            if (!filter.Matches(message.Address)) continue;
            _sender.SendSyncFireAndForget(filter.Ip, filter.Port, message.Address, message.Arguments);
        }
    }

    private void ReloadValidRelayFilters(List<OscRelayFilterModel> filterModels)
    {
        const string OSC_TEST_ADDRESS = "/osctest123";

        _logger.Information("Loading relay filters...");
        _filters?.Clear();
        List<OscReadonlyRelayFilter> filters = [];
        if (filterModels.Count == 0)
        {
            _logger.Information("No relay filters found");
            return;
        }

        foreach (var filterModel in filterModels)
        {
            var readonlyFilter = new OscReadonlyRelayFilter(filterModel);
            _logger.Debug("Checking validity of Relay Filter \"{filterName}\"", readonlyFilter.Name);

            var result = !string.IsNullOrWhiteSpace(readonlyFilter.Ip)
                && !string.IsNullOrWhiteSpace(readonlyFilter.Name)
                && readonlyFilter.Port != ushort.MinValue
                && _sender.SendSync(readonlyFilter.Ip, readonlyFilter.Port, OSC_TEST_ADDRESS, false);
            if (!result)
            {
                _logger.Warning("Skipping creation of listener \"{filterName}\" as its values are invalid (Name / Port / Ip / Filters)", readonlyFilter.Name);
                filterModel.SetValidity(false);
                continue;
            }

            filterModel.SetValidity(true);
            filters.Add(readonlyFilter);
            _logger.Debug("Relay Filter \"{filterName}\" is valid and has been added to list", readonlyFilter.Name);
        }

        _filters = filters;
        _logger.Information("{currentFilterCount}/{allFiltersCount} filters have been loaded", filters.Count, filterModels.Count);

        var invalidFilters = GetInvalidFilterNames();
        if (invalidFilters.Length > 0)
        {
            var filterString = $"The following filters are invalid: {string.Join(", ", invalidFilters)}";
            var argEx = new ArgumentException(filterString);
            SetFaultLogAndNotify(argEx, _logger, _notify, filterString);
        } else
        {
            SetFault(null);
        }
    }

    public string[] GetInvalidFilterNames()
    {
        return _config.Osc_Relay_Filters
            .Where(x => !x.GetValidity())
            .Select(x => x.Name)
            .ToArray();
    }
    #endregion
}