using HoscyCore.Configuration.Modern;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace HoscyCore;

public class HoscyCoreAppStartParameters()
{
    public Action<string>? OnProgress { get; set; }
    public bool CreateLoggerFromConfiguration { get; set; } = true;
    public bool CreateNewConfigIfMissing { get; set; } = true;
    public bool ShouldOpenConsoleIfRequested { get; set; } = true;
    public bool DisableConsoleLog { get; set; } = false;
    public Action<IServiceCollection>? AdditionalContainerInserts { get; set; }
    public Action<ILogger>? OnNewLoggerCreated { get; set; }
    public ConfigModel? PreloadedConfig { get; set; } = null;
}