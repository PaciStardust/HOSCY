using System.Timers;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Misc;

[PrototypeLoadIntoDiContainer(typeof(IAfkService))]
public class AfkService(ConfigModel config, IOutputManagerService output, ILogger logger) : StartStopServiceBase, IAfkService
{
    private readonly ConfigModel _config = config;
    private readonly IOutputManagerService _output = output;
    private readonly ILogger _logger = logger.ForContext<AfkService>();

    private System.Timers.Timer? _afkTimer;
    private DateTime _afkStarted = DateTime.Now;
    private uint _afkTimesChecked = 0;
    private static readonly OutputNotificationPriority _afkNotificationPriority = OutputNotificationPriority.Important;
    private static readonly OutputSettingsFlags _outputFlags = OutputSettingsFlags.AllowTextOutput;

    #region AFK
    public void StartAfk()
    {
        if (IsProcessing())
        {
            _logger.Information("AFK timer can not be started when already running");
            return;
        }
        if (!_config.Afk_ShowDuration)
        {
            _logger.Warning("AFK timer was not started as it was disabled");
            return;
        }

        _logger.Information("Starting AFK timer");
        _output.SendNotification(_config.Afk_StartText, _afkNotificationPriority, _outputFlags);
        _afkStarted = DateTime.Now;
        _afkTimesChecked = 0;

        _afkTimer = new(_config.Afk_BaseDurationDisplayIntervalSeconds * 1000);
        _afkTimer.Elapsed += AfkTimerElapsed;
        _afkTimer.Start();
        _logger.Information("AFK timer started");
    }

    public void StopAfk()
    {
        _logger.Information("Stopping AFK timer");
        _output.SendNotification(_config.Afk_StopText, _afkNotificationPriority, _outputFlags);
        _afkTimer?.Stop();
        _afkTimer?.Dispose();
        _afkTimer = null;
        _logger.Information("Stopped AFK timer");
    }

    private void AfkTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_config.Afk_TimesDisplayedBeforeDoublingInterval > 0)
        {
            _afkTimesChecked++;
            var cycle = (_afkTimesChecked / _config.Afk_TimesDisplayedBeforeDoublingInterval) + 1;
            var modulo = Math.Pow(2, (int)Math.Log(cycle, 2));

            if (_afkTimesChecked % modulo != 0)
                return;
        }

        var time = (e.SignalTime.AddMilliseconds(500) - _afkStarted).ToString(@"hh\:mm\:ss");
        _logger.Debug("Displaying AFK timer at {afkTime}", time);
        var message = $"{_config.Afk_StatusText} {time}";
        _output.SendNotification(message, _afkNotificationPriority, _outputFlags);
    }
    #endregion

    #region Start/Stop
    protected override bool IsStarted()
        => true;
    protected override bool IsProcessing()
        => _afkTimer is not null;

    public override void Restart()
        => RestartSimple(GetType(), _logger);

    protected override void StartInternal()
    {
        _logger.Information("AfkService start/stop only exists for shutdown cleanup!");
    }

    public override void Stop()
    {
        _logger.Information("Stopping AfkService, stopping timer if needed");
        StopAfk();
        _logger.Information("Stopped AfkService");
    }
    #endregion
}