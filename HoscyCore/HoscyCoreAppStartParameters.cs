using HoscyCore.Configuration.Modern;
using Microsoft.Extensions.DependencyInjection;

namespace HoscyCore;

public class HoscyCoreAppStartParameters()
{
    public Action<string>? OnProgress { get; set; }
    public bool CreateLoggerFromConfiguration { get; set; } = true;
    public bool CreateNewConfigIfMissing { get; set; } = true;
    public bool ShouldOpenConsoleIfRequested { get; set; } = true;
    public Action<IServiceCollection>? AdditionalContainerInserts { get; set; }
    public ConfigModel? PreloadedConfig { get; set; } = null;
}