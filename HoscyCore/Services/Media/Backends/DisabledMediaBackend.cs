using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Media.Backends;

[LoadIntoDiContainer(typeof(DisabledMediaBackendStartInfo))]
public class DisabledMediaBackendStartInfo : IMediaBackendStartInfo
{
    public MediaBackendConfigFlags ConfigFlags => MediaBackendConfigFlags.None;
    public string Name => "Disabled";
    public string Description => "Disables the Media backend, use this if you do not want media controls or are having issues";
    public Type ModuleType => typeof(DisabledMediaBackend);
}

[LoadIntoDiContainer(typeof(DisabledMediaBackend), Lifetime.Transient)]
public class DisabledMediaBackend(ILogger logger) : MediaBackendBase(logger.ForContext<DisabledMediaBackend>())
{
    protected override bool UseAlreadyStartedProtection => true;

    public override bool CanGetEndpoints => false;
    public override Task<Res<string[]>> GetEndpointNamesAsync()
        => Task.FromResult(ResC.TOk<string[]>([]));

    public override Task<Res> NextAsync()
        => CommandError("Next");

    public override Task<Res> PauseAsync()
        => CommandError("Pause");

    public override Task<Res> PlayAsync()
        => CommandError("Play");

    public override Task<Res> PlayPauseAsync()
        => CommandError("Toggle");

    public override Task<Res> PreviousAsync()
        => CommandError("Previous");

    private Task<Res> CommandError(string action) 
        => Task.FromResult(ResC.FailLog($"Unable to execute media command \"{action}\", backend disabled", _logger, lvl: ResMsgLvl.Warning));

    protected override void DisposeCleanup() { }

    protected override bool IsProcessing() => true;
    protected override bool IsStarted() => true;

    protected override Res StartForService()
        => ResC.Ok();

    protected override Res StopForModule()
        => ResC.Ok();
}