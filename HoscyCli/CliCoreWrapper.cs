using System.Text;
using System.Text.RegularExpressions;
using HoscyCli.Commands.Core;
using HoscyCli.Commands.Modules;
using HoscyCore;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Network;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli;

public class CliCoreWrapper
{
    private HoscyCoreApp? _coreApp = null;
    private ILogger? _logger = null;
    private UpdateInfo? _updateInfo = null;

    public Res<ResMsg[]> Start()
    {
        if (_coreApp is not null) return ResC.TOk<ResMsg[]>([]);
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
        if (!containerRes.IsOk) return ResC.TFail<ResMsg[]>(containerRes.Msg);

        var loggerRes = containerRes.Value.GetRequiredService<ILogger>();
        if (!loggerRes.IsOk) return ResC.TFail<ResMsg[]>(loggerRes.Msg);

        _logger = loggerRes.Value.ForContext<CliCoreWrapper>();
        return res;
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

        CheckUpdateIfNeeded(containerResult.Value, notify.Value);

        while(true)
        {
            if (_updateInfo is not null)
            {
                Console.WriteLine($"\nUpdate to version {_updateInfo.VersionNew} is available, type \"upd open\" or \"upd close\".");
            }
            Console.Write($"\nEnter a command to execute ('help' for help, 'exit' to exit)\n {verb} > ");
            var input = Console.ReadLine();
            Console.WriteLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (_updateInfo is not null && HandleUpdateCommandIfNeeded(input)) continue;
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

    private void CheckUpdateIfNeeded(DiContainer container, IBackToFrontNotifyService notify)
    {
        var config = container.GetRequiredService<ConfigModel>();
        if (!config.IsOk)
        {
            notify.SendResult("Can Not Grab Config for Update Check", config.Msg);
            return;
        }
        if (!config.Value.Debug_CheckForUpdatesOnStartup) return;

        var repo = container.GetRequiredService<IRepoService>();
        if (!repo.IsOk)
        {
            notify.SendResult("Can Not Grab RepoService for Update Check", repo.Msg);
            return;
        }

        _logger?.Debug("Running update check in background");
        repo.Value.CheckForUpdates().ContinueWith(x => OnUpdateCheckComplete(x, notify)).RunWithoutAwait();
    }

    private void OnUpdateCheckComplete(Task<Res<UpdateInfo>?> task, IBackToFrontNotifyService notify)
    {
        if (task.IsFaulted)
        {
            notify.SendError("Update Checker Task Failed", "The task for cheching for updates failed", task.Exception);
            return;
        }
        if (!task.IsCompletedSuccessfully || task.Result is null) return;
        
        if (!task.Result.IsOk)
        {
            notify.SendResult("Update Checker Task Failed", task.Result.Msg);
            return;
        }

        _updateInfo = task.Result.Value;
    }

    private bool HandleUpdateCommandIfNeeded(string input)
    {
        if (input.Equals("upd close", StringComparison.OrdinalIgnoreCase))
        {
            _updateInfo = null;
            return true;
        }

        if (!input.Equals("upd open", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var sb = new StringBuilder($"Upgrade from Version {_updateInfo!.VersionCurrent} => {_updateInfo.VersionNew}");

        if (_updateInfo.Title is not null)
        {
            sb.Append($"\n\n{_updateInfo.Title}");
        }
        if (_updateInfo.Text is not null)
        {
            sb.Append($"\n\n{_updateInfo.Text}");
        }
        Console.WriteLine(sb.ToString());

        if (_updateInfo.Url is not null)
        {
            Console.Write($"\nUpdate can be found at \"{_updateInfo.Url}\", type 'o' to open link > ");
            var key = Console.ReadLine();

            if (key?.Equals("o", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                OtherUtils.OpenWeb(_updateInfo.Url, _logger);
            }
        }        

        _updateInfo = null;
        return true;
    }
}