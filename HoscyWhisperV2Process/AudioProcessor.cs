using HoscyCore.Services.Recognition.Extra;
using Serilog;
using WebRtcVadSharp;

namespace HoscyWhisperV2Process;

public class AudioProcessor(WebRtcVad vad, WhisperIpcConfig config) //todo: logging
{
    private readonly WebRtcVad _vad = vad;
    private readonly WhisperIpcConfig _config = config;

    private const uint POINTS_GRACE_BOUNDARY_INCREASE = 2;
    private const uint POINTS_GRACE_DEDUCT = 3;

    private readonly uint _pointsSilenceBoundaryGraceLimit = config.Input_GraceFramesForIrregularitiesBoundary * POINTS_GRACE_DEDUCT;
    private readonly uint _pointsSilenceMiddleGraceLimit = config.Input_GraceFramesForIrregularitiesMiddle * POINTS_GRACE_DEDUCT;
    private readonly uint _consecutiveSilentFramesForProcessing = (uint)Math.Ceiling(config.Input_GraceFramesForIrregularitiesBoundary / 2f);
    private readonly uint _abruptCancelFrameLimit = config.Input_MaxRecognitionFrames + config.Input_RecognitionFrameInterval * 4;

    private uint _framesHandled = 0;
    private uint _pointsGrace = 0;
    private uint _consecutiveSilentFrames = 0;
    private uint _nextProcessingAt = config.Input_MinimumConsecutiveAudioFrames;
    private uint _lastProcessedAt = 0;

    private FrameProcessingResult Reset(bool shouldProcessIfCancelled)
    {
        var doProcess = shouldProcessIfCancelled && IsLongEnoughAfterLastSend();

        _framesHandled = 0;
        _pointsGrace = 0;
        _consecutiveSilentFrames = 0;
        _nextProcessingAt = _config.Input_MinimumConsecutiveAudioFrames;
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

        _nextProcessingAt = _framesHandled + _config.Input_RecognitionFrameInterval;
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
        var limit = _framesHandled < _config.Input_MinimumConsecutiveAudioFrames || _framesHandled >= _config.Input_MaxRecognitionFrames
            ? _pointsSilenceBoundaryGraceLimit
            : _pointsSilenceMiddleGraceLimit;

        return HandleGracePoints(hasAudio, limit, _framesHandled >= _config.Input_MinimumConsecutiveAudioFrames);
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
        return _framesHandled >= _lastProcessedAt + _config.Input_GraceFramesForIrregularitiesBoundary * 2;
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