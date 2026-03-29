using System.Diagnostics;
using System.Runtime.InteropServices;
using HoscyCore.Configuration.Modern;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore;

internal class HoscyCoreAppDebug(ILogger logger)
{
    private readonly ILogger _logger = logger.ForContext<HoscyCoreAppDebug>();
    private Process? _debugProcess = null;
    private bool _started = false;

    public void Start(HoscyCoreAppStartParameters startParameters, ConfigModel config)
    {
        if(_started)
        {
            _logger.Debug("Debug already started, skipping start");
            return;
        }
        _started = true;

        if (!startParameters.ShouldOpenConsoleIfRequested) return;

        if (config.Debug_LogViaCmdOnWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.Information("Starting windows console logger");
            WinApi.OpenConsole();
            _logger.Debug("Started windows console logger");
            return;
        }

        if (config.Debug_LogViaFileFollow)
        {
            if (string.IsNullOrWhiteSpace(config.Debug_LogFileFollowCommand) || string.IsNullOrWhiteSpace(config.Debug_LogFileFollowProcess))
            {
                _logger.Warning("LogViaFileFollow was enabled but no command or process set");
                return;
            }

            if (!config.Debug_LogFileFollowCommand.Contains("[LOGFILE]"))
            {
                _logger.Warning("LogFileFollowCommand does not contain file token \"[LOGFILE]\"");
                return;
            }

            _logger.Information("Starting debug terminal...");
            try
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = config.Debug_LogFileFollowProcess,
                    Arguments = config.Debug_LogFileFollowCommand.Replace("[LOGFILE]", LogUtils.LogFileName),
                    UseShellExecute = true,
                };
                _debugProcess = Process.Start(startInfo);
                _logger.Debug("Started debug terminal");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to start debug terminal");
            }
            return;
        }

        return;
    }

    public void Stop()
    {
        if (_debugProcess is null) return;
        _logger.Information("Stopping debug process...");
        try
        {
            _debugProcess.Kill();
            _debugProcess.WaitForExit();
            _debugProcess.Dispose();
            _debugProcess = null;
            _logger.Debug("Stopped debug process");
        } catch (Exception ex)
        {
            _logger.Error(ex, "Unable to stop debug process");
        }
    }
}