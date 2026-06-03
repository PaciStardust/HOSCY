using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(WebRtcCommandModule))]
public class WebRtcCommandModule
(   
    ReflectPropEditCommandModule reflectCm
) 
: AttributeCommandModule, ICoreCommandModule
{
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;

    public string ModuleName => "WebRtc";
    public string ModuleDescription => "Configure WebRtc for supported microphones";
    public string[] ModuleCommands => [ "webrtc" ];

    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["enabled"], nameof(ConfigModel.WebRtc_Enabled), ConfigModel.DESC_WebRtc_Enabled),
            new(["use-echo-cancellation"], nameof(ConfigModel.WebRtc_UseEchoCancellation), ConfigModel.DESC_WebRtc_UseEchoCancellation),
            new(["echo-cancellation-delay-ms"], nameof(ConfigModel.WebRtc_EchoCancellationDelayMs), ConfigModel.DESC_WebRtc_EchoCancellationDelayMs),
            new(["use-noise-suppression"], nameof(ConfigModel.WebRtc_UseNoiseSuppression), ConfigModel.DESC_WebRtc_UseNoiseSuppression),
            new(["noise-suppression-level"], nameof(ConfigModel.WebRtc_NoiseSuppressionLevel), ConfigModel.DESC_WebRtc_NoiseSuppressionLevel),
            new(["use-automatic-gain"], nameof(ConfigModel.WebRtc_UseAutomaticGainControl), ConfigModel.DESC_WebRtc_UseAutomaticGainControl),
            new(["use-highpass"], nameof(ConfigModel.WebRtc_UseHighPassFilter), ConfigModel.DESC_WebRtc_UseHighPassFilter),
            new(["use-preamp"], nameof(ConfigModel.WebRtc_UsePreAmplifier), ConfigModel.DESC_WebRtc_UsePreAmplifier),
            new(["preamp-gain-factor"], nameof(ConfigModel.WebRtc_PreAmplifierGainFactor), ConfigModel.DESC_WebRtc_PreAmplifierGainFactor)
        ];
    }
}