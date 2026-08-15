#if WINDOWS
#pragma warning disable CA1416 // Validate platform compatibility

using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Voice.Modules;

[LoadIntoDiContainer(typeof(WindowsVoiceModuleStartInfo))]
public class WindowsVoiceModuleStartInfo : IVoiceModuleStartInfo
{
    public VoiceModuleConfigFlags ConfigFlags => VoiceModuleConfigFlags.Windows;
    public string Name => "Windows";
    public string Description => "Voice synthesis using Windows";
    public Type ModuleType => typeof(WindowsVoiceModule);
    public ModulePriority Priority => ModulePriority.Low;
}

[PrototypeLoadIntoDiContainer(typeof(WindowsVoiceModule), Lifetime.Transient)]
public class WindowsVoiceModule(
    ConfigModel config,
    ILogger logger
) 
    : VoiceModuleBase(logger.ForContext<WindowsVoiceModule>())
{
    #region Injected
    private readonly ConfigModel _config = config;
    #endregion

    #region Vars
    private SpeechSynthesizer? _synth = null;
    #endregion

    #region Start / Stop
    protected override Res StartForService()
    {
        var synthRes = ResC.TWrapR(() => new SpeechSynthesizer(), "Failed to create voice synyh", _logger);
        if (!synthRes.IsOk) return ResC.Fail(synthRes.Msg);
        _synth = synthRes.Value;

        var voiceRes = WinApi.GetWindowsVoices(_synth);
        if (voiceRes.Count == 0)
            return ResC.FailLog("Could not find any voices to use", _logger);

        var voiceMatch = voiceRes.FirstOrDefault(x => x.Id == _config.Voice_Microsoft_ModelName);
        if (voiceMatch is null)
        {
            _logger.Warning("Failed to find voice matching ID {id}", _config.Voice_Microsoft_ModelName);
            voiceMatch = voiceRes[0];
        }
        _synth.SelectVoice(voiceMatch.Name);

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForModule() { return ResC.Ok(); }
    protected override void DisposeCleanup()
    {
        _synth?.Dispose();
        _synth = null;
    }

    protected override bool IsStarted() => _synth is not null;
    protected override bool IsProcessing() => IsStarted();
    #endregion

    #region Speaking
    public override async Task<Res> CreateAudio(string message, Stream stream, CancellationToken ct)
    {
        if (_synth == null)
            return ResC.FailLog("Unable to create audio, synth is not initialized", _logger);

        _synth.SetOutputToNull();
        _synth.SetOutputToWaveStream(stream);
        var prompt = _synth.SpeakAsync(message);
        while(!prompt.IsCompleted)
        {
            if (ct.IsCancellationRequested)
            {
                _synth.SpeakAsyncCancelAll();
            }
            await Task.Delay(25, CancellationToken.None);
        }
        _synth.SetOutputToNull();
        return ResC.Ok();
    }
    #endregion
}

#pragma warning restore CA1416 // Validate platform compatibility
#endif