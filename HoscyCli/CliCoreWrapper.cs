using System.Text.RegularExpressions;
using HoscyCli.Commands.Core;
using HoscyCli.Commands.Modules;
using HoscyCore;
using HoscyCore.Services.Interfacing;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli;

public class CliCoreWrapper
{
    private HoscyCoreApp? _coreApp = null;
    private ILogger? _logger = null;

    public void Start()
    {
        if (_coreApp is not null) return;
        _logger = LogUtils.CreateTemporaryLogger<CliCoreWrapper>(disableConsoleLogging: true);

        _coreApp = new HoscyCoreApp(_logger);
        var coreAppParams = new HoscyCoreAppStartParameters()
        {
            OnProgress = new((s) => Console.WriteLine($"Loading: {Regex.Replace(s, @"\r?\n", " ")}")),
            ShouldOpenConsoleIfRequested = true,
            DisableConsoleLog = true
        };
        _coreApp.Start(coreAppParams);
        _logger = _coreApp.GetContainer().GetRequiredService<ILogger>().ForContext<CliCoreWrapper>();
    }

    public void RunLoop()
    {
        if (_coreApp is null) throw new ArgumentNullException(nameof(_coreApp));
        _logger?.Information("Running CLI loop...");
        var commandModule = _coreApp.GetContainer().GetRequiredService<MainCommandModule>();
        var verb = " ";

        var notify = _coreApp.GetContainer().GetRequiredService<IBackToFrontNotifyService>();
        notify.OnNotificationSent += HandleNotification;

        while(true)
        {
            Console.Write($"\nEnter a command to execute ('help' for help, 'exit' to exit)\n {verb} > ");
            var input = Console.ReadLine();
            Console.WriteLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
            _logger?.Debug("Running command: \"{input}\"", input);

            CommandResult result;
            try
            {
                result = commandModule.Execute(input);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to execute command");
                Util.DisplayEx(e);
                result = CommandResult.Error;
            }

            verb = result switch
            {
                CommandResult.Success => " ",
                CommandResult.Error => "Error",
                CommandResult.NotFound => "Not Found",
                CommandResult.MissingParameter => "Param Missing",
                _ => "_"
            };
        }

        notify.OnNotificationSent -= HandleNotification;

        _logger?.Information("Stopping CLI loop...");
    }

    private void HandleNotification(object? sender, BackToFrontNotifyEventArgs e)
    {
        Console.WriteLine($"Notification ({(sender is null ? string.Empty : $"{sender.GetType().Name} ")}{e.Level}): {e.Title} - {e.Content}");
    }

    public void Stop()
    {
        _logger?.Information("Stopping CLI...");
        _coreApp?.Stop();
        _coreApp = null;
    }
}