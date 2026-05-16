using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(WebRtcCommandModule))]
public class WebRtcCommandModule
(   
    ReflectPropEditCommandModule _reflectCm
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = _reflectCm;

    public string ModuleName => "WebRtc";
    public string ModuleDescription => "Configure WebRtc for supported microphones";
    public string[] ModuleCommands => [ "webrtc" ];

    [SubCommandModule(["enabled"], "Should WebRtc be used on supported microphones")]
    public Res CmdEnabled()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_Enabled));
    }

    [SubCommandModule(["use-echo-cancellation"], "Enable Echo Cancellation")]
    public Res CmdUseEchoCancellation()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_UseEchoCancellation));
    }

    [SubCommandModule(["echo-cancellation-delay-ms"], "Delay with echo cancellation in MS")]
    public Res CmdEchoCancellationDelayMs()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_EchoCancellationDelayMs));
    }

    [SubCommandModule(["use-noise-suppression"], "Enable Noise Suppression")]
    public Res CmdUseNoiseSuppression()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_UseNoiseSuppression));
    }

    [SubCommandModule(["noise-suppression-level"], "Noise Suppression level")]
    public Res CmdNoiseSuppressionLevel()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_NoiseSuppressionLevel));
    }

    [SubCommandModule(["use-automatic-gain"], "Enable Automatic Gain")]
    public Res CmdUseAutomaticGain()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_UseAutomaticGainControl));
    }

    [SubCommandModule(["use-highpass"], "Enable Highpass Filter")]
    public Res CmdUseHighpass()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_UseHighPassFilter));
    }

    [SubCommandModule(["use-preamp"], "Enable Preamplifier")]
    public Res CmdUsePreamp()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_UsePreAmplifier));
    }

    [SubCommandModule(["preamp-gain-factor"], "Preamp gain factor")]
    public Res CmdPreampGainFactor()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Audio_WebRtc_PreAmplifierGainFactor));
    }
}