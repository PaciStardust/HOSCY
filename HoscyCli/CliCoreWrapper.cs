using System.Diagnostics;
using System.Runtime.InteropServices;
using HoscyCli.Commands.Core;
using HoscyCli.Commands.Modules;
using HoscyCore;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli;

public class CliCoreWrapper
{
    private HoscyCoreApp? _coreApp = null;
    private Process? _debugProcess = null;
    private ILogger? _logger = null;

    public void Start()
    {
        if (_coreApp is not null) return;
        _logger = LogUtils.CreateTemporaryLogger<CliCoreWrapper>(disableConsoleLogging: true);

        #if DEBUG
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            StartDebugTerminal(_logger);
        }
        #endif

        _coreApp = new HoscyCoreApp(_logger);
        var coreAppParams = new HoscyCoreAppStartParameters()
        {
            OnProgress = new((s) => Console.WriteLine($"Loading: {s.Replace(Environment.NewLine, " ")}")),
            ShouldOpenConsoleIfRequested = false,
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

        while(true)
        {
            Console.Write($"\nEnter a command to execute ('help' for help, 'exit' to exit)\n {verb} > ");
            var input = Console.ReadLine();
            Console.WriteLine();
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
            _logger?.Information($"Running command: {input}");

            CommandResult result;
            try
            {
                result = commandModule.Execute(input);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to execute command");
                Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
                result = CommandResult.Error;
            }

            verb = result switch
            {
                CommandResult.Success => " ",
                CommandResult.Error => "!",
                CommandResult.NotFound => "?",
                CommandResult.MissingParameter => "~",
                _ => "_"
            };
        }
        _logger?.Information("Stopping CLI loop...");
    }

    public void Stop()
    {
        _logger?.Information("Stopping CLI...");
        _coreApp?.Stop();
        _coreApp = null;
        _debugProcess?.Kill();
        _debugProcess?.Dispose();
        _debugProcess = null;
    }

    private void StartDebugTerminal(ILogger logger)
    {
        if (_debugProcess is not null) return;
        logger.Information("Starting debug terminal...");
        Console.WriteLine("Type your preferred terminal to follow logs (ex: 'foot')");
        var terminal = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(terminal)) return;

        var startInfo = new ProcessStartInfo()
        {
            FileName = terminal,
            Arguments = $"-e tail -f {LogUtils.LogFileName}",
            UseShellExecute = true
        };
        var process = Process.Start(startInfo);
        _debugProcess = process;
    }
}