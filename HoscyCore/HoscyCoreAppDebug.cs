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

    public Res Start(HoscyCoreAppStartParameters startParameters, ConfigModel config)
    {
        if(_started)
        {
            _logger.Debug("Debug already started, skipping start");
            return ResC.Ok();
        }
        _started = true;

        if (!startParameters.ShouldOpenConsoleIfRequested) return ResC.Ok();

        if (config.Debug_LogViaCmdOnWindows && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.Information("Starting windows console logger");
            var winConsoleRes = ResC.WrapR(WinApi.OpenConsole, "Failed opening console on windows", _logger);
            if (winConsoleRes.IsOk)
            {
                _logger.Debug("Started windows console logger");
            }
            return winConsoleRes;
        }

        if (config.Debug_LogViaFileFollow)
        {
            if (string.IsNullOrWhiteSpace(config.Debug_LogFileFollowCommand) || string.IsNullOrWhiteSpace(config.Debug_LogFileFollowProcess))
            {
                _logger.Warning("LogViaFileFollow was enabled but no command or process set");
                return ResC.Ok();
            }

            if (!config.Debug_LogFileFollowCommand.Contains("[LOGFILE]"))
            {
                _logger.Warning("LogFileFollowCommand does not contain file token \"[LOGFILE]\"");
                return ResC.Ok();
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
                return ResC.FailLog("Unable to start debug terminal", _logger, ex);
            }

            return ResC.Ok();
        }

        return ResC.Ok();
    }

    public Res Stop()
    {
        if (_debugProcess is null) return ResC.Ok();

        _logger.Information("Stopping debug process...");
        try
        {
            _debugProcess.Kill();
            _debugProcess.WaitForExit();
            _logger.Debug("Stopped debug process");
        } 
        catch (Exception ex)
        {
            return ResC.FailLog("Unable to stop debug process", _logger, ex);
        }
        finally
        {
            _debugProcess.Dispose();
            _debugProcess = null;
        }

        return ResC.Ok();
    }
}