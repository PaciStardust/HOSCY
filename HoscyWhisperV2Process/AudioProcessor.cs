using Serilog;
using WebRtcVadSharp;

namespace HoscyWhisperV2Process;

public class AudioProcessor(WebRtcVad vad, SilenceDetectorConfiguration config)
{
    private readonly WebRtcVad _vad = vad;
    private readonly SilenceDetectorConfiguration _config = config;

    private const uint POINTS_GRACE_BOUNDARY_INCREASE = 2;
    private const uint POINTS_GRACE_DEDUCT = 3;

    private readonly uint _pointsSilenceBoundaryGraceLimit = config.GraceFramesForIrregularitiesBoundary * POINTS_GRACE_DEDUCT;
    private readonly uint _pointsSilenceMiddleGraceLimit = config.GraceFramesForIrregularitiesMiddle * POINTS_GRACE_DEDUCT;
    private readonly uint _consecutiveSilentFramesForProcessing = (uint)Math.Ceiling(config.GraceFramesForIrregularitiesBoundary / 2f);
    private readonly uint _abruptCancelFrameLimit = config.MaxRecognitionFrames + config.RecognitionFrameInterval * 4;

    private uint _framesHandled = 0;
    private uint _pointsGrace = 0;
    private uint _consecutiveSilentFrames = 0;
    private uint _nextProcessingAt = config.MinimumConsecutiveAudioFrames;
    private uint _lastProcessedAt = 0;

    private FrameProcessingResult Reset(bool shouldProcessIfCancelled)
    {
        var doProcess = shouldProcessIfCancelled && IsLongEnoughAfterLastSend();

        _framesHandled = 0;
        _pointsGrace = 0;
        _consecutiveSilentFrames = 0;
        _nextProcessingAt = _config.MinimumConsecutiveAudioFrames;
        _lastProcessedAt = 0;

        return doProcess
            ? FrameProcessingResult.CancelAndProcess
            : FrameProcessingResult.Cancel;
    }

    public FrameProcessingResult Process10msFrame(Span<byte> audioFrame)
    {
        var internalResult = ProcessFrameInternal(audioFrame);
        if (internalResult != FrameProcessingResult.ContinueAndProcess)
        {
            return internalResult;
        }

        if (_framesHandled < _nextProcessingAt)
        {
            return FrameProcessingResult.Continue;
        }

        _nextProcessingAt = _framesHandled + _config.RecognitionFrameInterval;
        _lastProcessedAt = _framesHandled;
        return FrameProcessingResult.ContinueAndProcess;
    }

    private FrameProcessingResult ProcessFrameInternal(Span<byte> audioFrame)
    {
        var hasAudio = _vad.HasSpeech(audioFrame.ToArray());

        // If we are not actively processing and theres no audio, there is no need to do anything
        if (!hasAudio && _framesHandled == 0)
        {
            Reset(false);
            return FrameProcessingResult.Empty;
        }

        _framesHandled++;

        if (_framesHandled > _abruptCancelFrameLimit)
        {
            return Reset(true);
        }

        _consecutiveSilentFrames = hasAudio ? 0 : _consecutiveSilentFrames + 1;

        // If we are above or below a limit, we use the harsh grace limit, otherwise we use the more lenient one
        var limit = _framesHandled < _config.MinimumConsecutiveAudioFrames || _framesHandled >= _config.MaxRecognitionFrames
            ? _pointsSilenceBoundaryGraceLimit
            : _pointsSilenceMiddleGraceLimit;

        return HandleGracePoints(hasAudio, limit, _framesHandled >= _config.MinimumConsecutiveAudioFrames);
    }

    private FrameProcessingResult HandleGracePoints(bool hasAudio, uint graceLimit, bool shouldProcessIfCancelled)
    {
        if (hasAudio)
        {
            _pointsGrace = Math.Min(graceLimit, _pointsGrace + POINTS_GRACE_BOUNDARY_INCREASE);
            return FrameProcessingResult.Continue;
        }
        else
        {
            _pointsGrace -= Math.Min(POINTS_GRACE_DEDUCT, _pointsGrace);
            return _pointsGrace == 0
                ? Reset(shouldProcessIfCancelled)
                : _consecutiveSilentFrames >= _consecutiveSilentFramesForProcessing 
                    ? FrameProcessingResult.ContinueAndProcess
                    : FrameProcessingResult.Continue;
        }
    } 

    private bool IsLongEnoughAfterLastSend()
    {
        return _framesHandled >= _lastProcessedAt + _config.GraceFramesForIrregularitiesBoundary * 2;
    }
} 

public enum FrameProcessingResult
{
    Empty,
    Continue,
    ContinueAndProcess,
    Cancel,
    CancelAndProcess
}

/// <summary>
/// Configuration for silence detection, an audio frame represents 10ms of audio
/// </summary>
public record SilenceDetectorConfiguration
{
    public const uint MS_IN_FRAME = 10;
    public uint MinimumConsecutiveAudioFrames { get; init => Math.Max(value, 100 / MS_IN_FRAME); } = 200 / MS_IN_FRAME;
    public uint GraceFramesForIrregularitiesMiddle { get; init => Math.Max(value, 250 / MS_IN_FRAME); } = 500 / MS_IN_FRAME;
    public uint GraceFramesForIrregularitiesBoundary { get; init => Math.Max(value, 20 / MS_IN_FRAME); } = 50 / MS_IN_FRAME;
    public uint RecognitionFrameInterval { get; init => Math.Max(value, 250 / MS_IN_FRAME); } = 500 / MS_IN_FRAME;
    public uint MaxRecognitionFrames { get; init => Math.Max(value, 8_000 / MS_IN_FRAME); } = 16_000 / MS_IN_FRAME;
}