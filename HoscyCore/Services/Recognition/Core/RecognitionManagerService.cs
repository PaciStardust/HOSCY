using System.Text.RegularExpressions;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Recognition.Core;

[PrototypeLoadIntoDiContainer(typeof(IRecognitionManagerService), Lifetime.Singleton)]
public class RecognitionManagerService //todo: [TEST] create test for this
(
    IBackToFrontNotifyService notify,
    ILogger logger,
    IContainerBulkLoader<IRecognitionModuleStartInfo> infoLoader,
    IContainerBulkLoader<IRecognitionModule> moduleLoader,
    ConfigModel config,
    IOutputManagerService output
) 
    : SoloModuleManagerBase<IRecognitionModuleStartInfo, IRecognitionModule>
        (notify, logger.ForContext<RecognitionManagerService>(), infoLoader, moduleLoader),
    IRecognitionManagerService
{
    #region Injected
    private readonly ConfigModel _config = config;
    private readonly IOutputManagerService _output = output;
    #endregion

    #region Module => Start / Stop
    protected override void OnModulePostStart(IRecognitionModule module)
    {
        UpdateSettings();

        module.OnSpeechActivity += HandleOnSpeechActivity;
        module.OnSpeechRecognized += HandleOnSpeechRecognized;

        bool listening = false;
        if (_config.Recognition_Mute_StartUnmuted)
        {
            listening = SetListeningInternal(module, true);
            if (!listening)
            {
                _logger.Warning("Failed to correctly set listening status on startup");
            }
        }
        InvokeModuleStatusChanged(listening, true);
    }

    protected override void OnModulePreStop(IRecognitionModule module)
    {
        module.SetListening(false);

        module.OnSpeechActivity -= HandleOnSpeechActivity;
        module.OnSpeechRecognized -= HandleOnSpeechRecognized;
    }

    protected override void OnModulePostStop()
    {
        InvokeModuleStatusChanged(false, false);
    }
    #endregion

    #region Module => Control
    public bool IsListening
        => _currentModule?.IsListening ?? false;

    public event EventHandler<RecognitionStatusChangedEventArgs> OnModuleStatusChanged = delegate {};
    private void InvokeModuleStatusChanged(bool listening, bool started)
    {
        _logger.Verbose("Triggering event for module status update started={started} listening={listening}", started, listening);
        OnModuleStatusChanged.Invoke(this, new(listening, started));
    }

    protected override string GetSelectedModuleName()
        => _config.Recognition_SelectedModuleName;

    public bool SetListening(bool state) //todo: [REFACTOR] Does not allow the module on its own to send updates
    {
        var res = SetListeningInternal(_currentModule, state);
        InvokeModuleStatusChanged(res, true);
        return res;
    }
    public bool SetListeningInternal(IRecognitionModule? module, bool state)
    {
        _logger.Debug("Setting listening to {state}", state);

        bool newState;
        if (module is null || module.GetCurrentStatus() == ServiceStatus.Stopped)
        {
            _logger.Warning("Unable to set listening, provided module is null or stopped");
            newState = false;
        }
        else
        {
            newState = module.SetListening(state);
        }

        _logger.Debug("Listening was requested to be set to {state} and is now {actualState}", state, newState);
        return newState;
    }
    #endregion

    #region Functionality
    private uint _messageIndex = 0;
    private void HandleOnSpeechRecognized(string message)
    {
        _messageIndex++;
        _logger.Verbose("Received unclean message {id} => \"{message}\"", _messageIndex, message);
        if (!CleanMessage(ref message))
        {
            _logger.Verbose("Cleaned message {id} => Is empty", _messageIndex);
            return;
        }
        _logger.Debug("Cleaned message {id} => \"{message}\"", _messageIndex, message);

        var flags = OutputSettingsFlags.None;

        if (_config.Recognition_Send_ViaText)
            flags |= OutputSettingsFlags.AllowTextOutput;
        if (_config.Recognition_Send_ViaAudio)
            flags |= OutputSettingsFlags.AllowAudioOutput;
        if (_config.Recognition_Send_ViaOther)
            flags |= OutputSettingsFlags.AllowOtherOutput;
        if (_config.Recognition_Send_DoTranslate)
            flags |= OutputSettingsFlags.DoTranslate;
        if (_config.Preprocessing_DoReplacementsPartial)
            flags |= OutputSettingsFlags.DoPreprocessPartial;
        if (_config.Recognition_Send_DoPreprocessFull)
            flags |= OutputSettingsFlags.DoPreprocessFull;

        _output.SendMessage(message, flags);
    }

    private Regex _inputDenoiseFilter = new(" *");
    private bool CleanMessage(ref string message)
    {
        message = _config.Recognition_Fixup_RemoveEndPeriod
            ? message.TrimStart().TrimEnd(' ', '.', '。')
            : message.Trim();

        var denoiseMatch = _inputDenoiseFilter.Match(message);
        if (!denoiseMatch.Success)
            return false;

        message = denoiseMatch.Groups[1].Value.Trim();

        if (_config.Recognition_Fixup_CapitalizeFirstLetter)
            message = message.FirstCharToUpper();

        return !string.IsNullOrWhiteSpace(message);
    }

    public void UpdateSettings()
    {
        var filterWords = _config.Recognition_Fixup_NoiseFilter.Select(x => $"(?:{Regex.Escape(x)})");
        var filterCombined = string.Join('|', filterWords);
        var regString = $"^(?:(?<=^| |\\b)(?:{filterCombined})(?=$| |\\b))?(.*?)(?:(?<=^| |\\b)(?:{filterCombined})(?=$| |\\b))?$";

        _logger.Information("Setting recognition denoiser to {regString}", regString);
        try
        {
            _inputDenoiseFilter = new Regex(regString, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to set denoiser to {regString}, it will not be overridden", regString);
            _notify.SendWarning("Recognition denoiser not loaded", $"Unable to load denoiser, it will not be used:\n{regString}");
        }
    }

    private void HandleOnSpeechActivity(bool state)
    {
        _logger.Verbose("Forwarding speech activity state {state} to output", state);
        _output.SetProcessingIndicator(state);
    }
    #endregion

    #region Overrides
    protected override bool ShouldStartModelOnStartup()
    {
        return _config.Recognition_AutoStart;
    }
    #endregion
}