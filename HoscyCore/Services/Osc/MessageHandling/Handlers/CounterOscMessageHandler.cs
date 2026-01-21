using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[PrototypeLoadIntoDiContainer(typeof(CounterOscMessageHandler))]
public class CounterOscMessageHandler(IOutputManagerService output, ConfigModel config, ILogger logger) : IOscMessageHandler //todo: [TEST] Does this trigger?
{
    private readonly IOutputManagerService _output = output;
    private readonly ConfigModel _config = config;
    private readonly ILogger _logger = logger.ForContext<CounterOscMessageHandler>();

    private DateTimeOffset _counterLastDisplay = DateTimeOffset.MinValue;

    public bool HandleMessage(OscMessage message)
    {
        var now = DateTimeOffset.UtcNow;

        var counterMatch = _config.Counters_List.FirstOrDefault(x => x.FullParameter().Equals(message.Address, StringComparison.OrdinalIgnoreCase));
        if (counterMatch is null)
            return false;

        if (!counterMatch.Enabled || (now - counterMatch.LastUsed).TotalSeconds < counterMatch.Cooldown)
            return true;

        counterMatch.Increase();
        _logger.Verbose("Counter \"{counterName}\" ({counterParameter}) increased to {counterCount}", counterMatch.Name, counterMatch.FullParameter(), counterMatch.Count);

        if (!counterMatch.DoDisplay || !_config.Counters_ShowNotification || (now - _counterLastDisplay).TotalSeconds < _config.Counters_DisplayCooldownSeconds)
            return true;

        var counterString = CreateCounterString(now);
        if (!string.IsNullOrWhiteSpace(counterString))
        {
            _counterLastDisplay = now;
            _logger.Debug("Sending counter notification \"{counterNotification}\"", counterString);
            _output.SendNotification(counterString, OutputNotificationPriority.Low, OutputSettingsFlags.AllowTextOutput);
        }
        return true;
    }

    private string CreateCounterString(DateTimeOffset now)
    {
        var earliest = now.AddSeconds(-_config.Counters_DisplayDurationSeconds);
        var matchingCounters = _config.Counters_List
            .Where(x => x.LastUsed >= earliest)
            .OrderByDescending(x => x.Count);
        return string.Join(", ", matchingCounters);
    }
}