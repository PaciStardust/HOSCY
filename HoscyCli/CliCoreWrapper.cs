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

    public Res Start()
    {
        if (_coreApp is not null) return ResC.Ok();
        _logger = LogUtils.CreateTemporaryLogger<CliCoreWrapper>(disableConsoleLogging: true);

        _coreApp = new HoscyCoreApp(_logger);
        var coreAppParams = new HoscyCoreAppStartParameters()
        {
            OnProgress = new((s) => Console.WriteLine($"Loading: {Regex.Replace(s, @"\r?\n", " ")}")),
            ShouldOpenConsoleIfRequested = true,
            DisableConsoleLog = true
        };
        var res = _coreApp.Start(coreAppParams);
        if (!res.IsOk) return res;

        var containerRes = _coreApp.GetContainer();
        if (!containerRes.IsOk) return ResC.Fail(containerRes.Msg);

        var loggerRes = containerRes.Value.GetRequiredService<ILogger>();
        if (!loggerRes.IsOk) return ResC.Fail(loggerRes.Msg);

        _logger = loggerRes.Value.ForContext<CliCoreWrapper>();
        return ResC.Ok();
    }

    public Res RunLoop()
    {
        if (_coreApp is null) return ResC.Fail("App is null");
        _logger?.Information("Running CLI loop...");

        var containerResult = _coreApp.GetContainer();
        if (!containerResult.IsOk) return ResC.Fail(containerResult.Msg);

        var cmdModule = containerResult.Value.GetRequiredService<MainCommandModule>();
        if (!cmdModule.IsOk) return ResC.Fail(cmdModule.Msg);

        var notify = containerResult.Value.GetRequiredService<IBackToFrontNotifyService>();
        if (!notify.IsOk) return ResC.Fail(notify.Msg);

        var verb = " ";
        notify.Value.OnNotificationSent += HandleNotification;

        while(true)
        {
            Console.Write($"\nEnter a command to execute ('help' for help, 'exit' to exit)\n {verb} > ");
            var input = Console.ReadLine();
            Console.WriteLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
            _logger?.Debug("Running command: \"{input}\"", input);

            var res = ResC.Wrap(() => cmdModule.Value.Execute(input), "Failed to execute command", _logger);
            if (!res.IsOk)
            {
                verb = "Error";
                Console.WriteLine(res.ToString());
            }
            else
            {
                verb = " ";
            }
        }

        notify.Value.OnNotificationSent -= HandleNotification;

        _logger?.Information("Stopping CLI loop...");
        return ResC.Ok();
    }

    private void HandleNotification(object? sender, BackToFrontNotifyEventArgs e)
    {
        Console.WriteLine($"Notification ({(sender is null ? string.Empty : $"{sender.GetType().Name} ")}{e.Level}): {e.Title} - {e.Content}");
    }

    public Res Stop()
    {
        _logger?.Information("Stopping CLI...");
        var res = _coreApp?.Stop();
        _coreApp = null;
        return res ?? ResC.Ok();
    }
}