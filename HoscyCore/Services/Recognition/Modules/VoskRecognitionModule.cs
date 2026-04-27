using System.Collections.Concurrent;
using System.Reflection;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Audio;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using Serilog;
using SoundFlow.Enums;
using Vosk;

namespace HoscyCore.Services.Recognition.Modules;

[PrototypeLoadIntoDiContainer(typeof(VoskRecognitionModuleStartInfo), Lifetime.Singleton)]
public class VoskRecognitionModuleStartInfo : IRecognitionModuleStartInfo
{
    public string Name => "Vosk Recognizer";
    public string Description => "Local AI, quality / RAM, VRAM usage varies, startup may take a while";
    public Type ModuleType => typeof(VoskRecognitionModule);

    public RecognitionModuleConfigFlags ConfigFlags 
        => RecognitionModuleConfigFlags.Microphone | RecognitionModuleConfigFlags.Vosk;
}

[PrototypeLoadIntoDiContainer(typeof(VoskRecognitionModule), Lifetime.Transient)]
public class VoskRecognitionModule(ILogger logger, ConfigModel config, IAudioService audio)
    : RecognitionModuleBase(logger.ForContext<VoskRecognitionModule>())
{
    #region Injects
    private readonly ConfigModel _config = config;
    private readonly IAudioService _audio = audio;
    #endregion

    #region Runtime Vars
    private AudioCaptureDeviceProxy? _mic = null;
    private VoskRecognizer? _rec = null;
    private Thread? _voskThread = null;
    private bool _shouldThreadStop = false;
    #endregion

    #region Start / Stop
    protected override bool IsStarted()
        => _mic is not null || _rec is not null || _voskThread is not null;

    protected override bool IsProcessing()
        => _mic is not null && _mic.IsStarted
        && _rec is not null
        && _voskThread is not null && _voskThread.IsAlive;

    protected override Res StartForService()
    {
        if (string.IsNullOrWhiteSpace(_config.Recognition_Vosk_CurrentModel))
            return ResC.FailLog("Unable to start recognition, Vosk Model not set", _logger);

        _logger.Information("Attempting to start with model \"{model}\"", _config.Recognition_Vosk_CurrentModel);

        if(!_config.Recognition_Vosk_Models.TryGetValue(_config.Recognition_Vosk_CurrentModel, out var voskModelPath))
            return ResC.FailLog($"Unable to start recognition, could not find selected Vosk Model \"{_config.Recognition_Vosk_CurrentModel}\"", _logger);

        if (!Directory.Exists(voskModelPath))
            return ResC.FailLog($"Unable to locate folder for Vosk Model at path \"{voskModelPath}\"", _logger);

        var model = ResC.TWrapR(() => new Model(voskModelPath),
            $"Failed to load Vosk Model from path \"{voskModelPath}\"", _logger);
        if (!model.IsOk) return ResC.Fail(model.Msg);

        try
        {
            //Using reflection to get handle (Checking if fails to initalize model)
            var bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

            var handleField = typeof(Model).GetField("handle", bindFlags);
            if (handleField is null) 
                return ResC.FailLog($"Failed to retrieve model handle field to validate loaded model. {MODEL_LOAD_ERROR}", _logger);

            var handleValue = handleField.GetValue(model.Value);
            if (handleValue is null)
                return ResC.FailLog($"Failed to retrieve value of model handle field to validate loaded model. {MODEL_LOAD_ERROR}", _logger);

            var modelhandleInternal = (System.Runtime.InteropServices.HandleRef)handleValue;
            if (modelhandleInternal.Handle == IntPtr.Zero)
                return ResC.FailLog($"Attempted to start model but the picked recognition model file is invalid. {MODEL_LOAD_ERROR}", _logger);
        }
        catch (Exception ex)
        {
            return ResC.FailLog($"Attempted to start model but the picked recognition model file is invalid. {MODEL_LOAD_ERROR}", _logger, ex);
        }

        var recog = ResC.TWrapR(() => new VoskRecognizer(model.Value, 16_000),
            "Failed to create instance of Vosk Recognizer", _logger);
        if (!recog.IsOk) return ResC.Fail(recog.Msg);
        _rec = recog.Value;

        var mic = _audio.CreateCaptureDeviceProxy();
        if (!mic.IsOk) return ResC.Fail(mic.Msg);
        _mic = mic.Value;

        _mic.OnAudioProcessed += HandleAudioProcessed;
        var micStart = _mic.Start();
        if (!micStart.IsOk) return micStart;

        _voskThread = new(new ThreadStart(HandleAvailableDataLoop))
        {
            Name = "Vosk Data Handler",
            Priority = ThreadPriority.AboveNormal
        };

        _shouldThreadStop = false;
        _voskThread.Start();

        Thread.Sleep(100);
        return _voskThread.IsAlive ? ResC.Ok() : ResC.FailLog("Vosk thread not alive after 100ms", _logger);
    }

    private const string MODEL_LOAD_ERROR = "Have you downloaded a compatible model, picked the correct folder and verified it is not corrupt?";
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForRecognitionModule()
    {
        _shouldThreadStop = true;
        _dataToHandle.Clear();

        List<ResMsg> _stopErrors = [];

        _mic?.Stop().IfFail(x => _stopErrors.Add(x.WithContext("Mic Stop")));

        ResC.WrapR(() => _voskThread?.Join(500), "Failed to join Vosk thread in 500ms", _logger)
            .IfFail(x => _stopErrors.Add(x.WithContext("Thread Stop")));

        return _stopErrors.Count == 0 ? ResC.Ok() : ResC.FailM(_stopErrors);
    }
    protected override void DisposeCleanup()
    {
        _mic?.Dispose();
        _mic = null;

        _voskThread = null;

        _rec?.Dispose();
        _rec = null;
    }
    #endregion

    #region Listening
    public override bool IsListening 
        => _mic?.IsListening ?? false;

    protected override Res<bool> SetListeningForRecognitionModule(bool state)
    {
        _mic?.SetListening(state);
        return ResC.TOk(IsListening);
    }
    protected override bool UseOnlySetListeningWhenStartedProtection => true;
    #endregion

    #region Handling
    private DateTimeOffset _lastChangedAt = DateTimeOffset.MaxValue;
    private string _lastChangedString = string.Empty;
    private readonly ConcurrentQueue<byte[]> _dataToHandle = [];

    private void HandleAudioProcessed(Span<byte> span, Capability capability)
    {
        _dataToHandle.Enqueue(span.ToArray());
    }   

    private void HandleAvailableDataLoop()
    {
        _logger.Information("Started Vosk recognizer data handling thread");
        ulong segmentId = 0;
        while (_rec is not null && !_shouldThreadStop)
        {
            if (!_dataToHandle.TryDequeue(out var bytes))
            {
                Thread.Sleep(10);
                continue;
            }

            if (bytes.Length == 0) continue;

            var acceptResult = ResC.TWrapR(() => _rec!.AcceptWaveform(bytes, bytes.Length),
                "Failed to parse waveform data", _logger, ResMsgLvl.Warning);

            if (!acceptResult.IsOk)
            {
                SetFault(acceptResult.Msg);
                Thread.Sleep(10000);
                continue;
            }

            segmentId++;
            var handleResult = acceptResult.Value ? HandleResultComplete(segmentId) : HandleResultPartial(segmentId);
            if (!handleResult.IsOk)
            {
                SetFault(handleResult.Msg);
                Thread.Sleep(10000);
                continue;
            }
        }
        _logger.Information("Stopped Vosk recognizer data handling thread");
    }

    private Res HandleResultComplete(ulong segmentId)
    {
        if (_rec is null)
        {
            _logger.Warning("Attempted to handle complete result with ID {id} on disabled recognizer", segmentId);
            return ResC.Ok();
        }

        _logger.Debug("Handling complete result with ID {id}", segmentId);

        var result = ResC.TWrapR(() => _rec.Result(), $"Failed to grab result with ID {segmentId}", _logger, ResMsgLvl.Warning);
        if (!result.IsOk) return ResC.Fail(result.Msg);

        var cleanResult = CleanMessage(result.Value, "text", false);
        if (!cleanResult.IsOk) return ResC.Fail(cleanResult.Msg);

        if (string.IsNullOrWhiteSpace(cleanResult.Value))
        {
            _logger.Debug("Complete result with ID {id} was empty", segmentId);
        }
        else
        {
            _logger.Debug("Complete result with ID {id} yielded the result: \"{result}\"", segmentId, cleanResult.Value);
            InvokeSpeechRecognized(cleanResult.Value);
        }

        return ClearLastChanged();
    }

    private Res HandleResultPartial(ulong segmentId)
    {
        if (_rec is null)
        {
            _logger.Warning("Attempted to handle partial result with ID {id} on disabled recognizer", segmentId);
            return ResC.Ok();
        }

        // _logger.Debug("Handling partial result with ID {id}", segmentId);

        var partialResult = ResC.TWrapR(() => _rec.PartialResult(),
            $"Failed to grab partial result with ID {segmentId}", _logger, ResMsgLvl.Warning);
        if (!partialResult.IsOk) return ResC.Fail(partialResult.Msg);

        var cleanResult = CleanMessage(partialResult.Value, "partial", true);
        if (!cleanResult.IsOk) return ResC.Fail(cleanResult.Msg);

        if (string.IsNullOrWhiteSpace(cleanResult.Value))
        {
            // _logger.Debug("Partial result with ID {id} was empty", segmentId);
            return ResC.Ok();
        }

        if (_lastChangedString != cleanResult.Value)
        {
            _lastChangedString = cleanResult.Value;
            _lastChangedAt = DateTimeOffset.UtcNow;
            InvokeSpeechActivity(true);
            return ResC.Ok();
        }

        if ((DateTimeOffset.UtcNow - _lastChangedAt).TotalMilliseconds > _config.Recognition_Vosk_NewWordWaitTimeMs)
        {
            _logger.Debug("Partial result with ID {id} yielded the result: \"{result}\"", segmentId, cleanResult.Value);
            InvokeSpeechRecognized(cleanResult.Value);
            return ClearLastChanged();
        }

        return ResC.Ok();
    }

    private Res ClearLastChanged()
    {
        InvokeSpeechActivity(false);
        _lastChangedString = string.Empty;
        _lastChangedAt = DateTime.MaxValue;
        return _rec is null 
            ? ResC.Ok() 
            : ResC.WrapR(() => _rec.Reset(), "Failed to reset recognizer", _logger, ResMsgLvl.Warning);
    }

    private Res<string> CleanMessage(string res, string field, bool extraFilter)
    {
        var extracted = OtherUtils.ExtractFromJson(field, res, _logger);

        return extraFilter && extracted.IsOk
            && _config.Recognition_Fixup_NoiseFilter.Any(x => x.Equals(extracted.Value, StringComparison.OrdinalIgnoreCase))
            ? ResC.TOk(string.Empty)
            : extracted;
    }
    #endregion
}