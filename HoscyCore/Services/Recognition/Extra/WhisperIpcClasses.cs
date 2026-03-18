using HoscyCore.Utility;
using Serilog.Events;

namespace HoscyCore.Services.Recognition.Extra;

public record WhisperIpcConfig
{
    public int ParentProcessId { get; init; } = 0;
    public string ParentSendingPipe { get; init; } = string.Empty;

    public string CaptureDeviceName { get; init; } = string.Empty;
    public WhisperIpcVadOperatingMode VadOperatingMode { get; init; } = WhisperIpcVadOperatingMode.Aggressive;

    public const uint MS_IN_FRAME = 10;
    public uint Input_MinimumConsecutiveAudioFrames { get; init => Math.Max(value, 100 / MS_IN_FRAME); } = 250 / MS_IN_FRAME;
    public uint Input_GraceFramesForIrregularitiesMiddle { get; init => Math.Max(value, 250 / MS_IN_FRAME); } = 500 / MS_IN_FRAME;
    public uint Input_GraceFramesForIrregularitiesBoundary { get; init => Math.Max(value, 20 / MS_IN_FRAME); } = 50 / MS_IN_FRAME;
    public uint Input_RecognitionFrameInterval { get; init => Math.Max(value, 250 / MS_IN_FRAME); } = 500 / MS_IN_FRAME;
    public uint Input_MaxRecognitionFrames { get; init => Math.Max(value, 4_000 / MS_IN_FRAME); } = 16_000 / MS_IN_FRAME; 

    public required string Whisper_ModelPath { get; init; }
    public string Whisper_Language { get; init; } = string.Empty;
    public bool Whisper_DetectLanguage { get; init; } = false;
    public bool Whisper_TranslateToEnglish { get; init; } = false;
    public string Whisper_Prompt { get; init; } = string.Empty;
    public bool Whisper_SingleSegment { get; init; } = false;
    public float Whisper_NoSpeechThreshold { get; init => value.MinMax(-1, 1); } = -1;
    public float Whisper_Temperature { get; init => value.MinMax(-1, 1); } = -1;
    public float Whisper_TemperatureInc { get; init => value.MinMax(-1, 1); } = -1;
    public float Whisper_MaxInitialT { get; init => value.MinMax(-1, 1); } = -1;
    public bool Whisper_SetThreads { get; init; } = false;
    public int Whisper_ThreadCount { get; init; } = -4;
    public int Whisper_MaxSegmentLength { get; init => value.MinMax(0, int.MaxValue); } = 0;
    public int Whisper_MaxTokensPerSegment { get; init => value.MinMax(0, int.MaxValue); } = 0;
    public bool Whisper_UseGreedySampling { get; init; } = false;
    public int Whisper_GreedyBestOf { get; init => value.MinMax(0, int.MaxValue); } = 0;
    public bool Whisper_UseBeamSearchSampling { get; init; } = false;
    public int Whisper_BeamSize { get; init => value.MinMax(0, 10); } = 0;
    public bool Whisper_UseGpu { get; init; } = true;
    public int Whisper_GpuId { get; init => value.MinMax(0, int.MaxValue); } = 0;
}

public enum WhisperIpcVadOperatingMode
{
    HighQuality,
    LowBitrate,
    Aggressive,
    VeryAggressive
}

public record WhisperIpcLog
{
    public const char IDENTIFIER = 'L';
    public required LogEventLevel LogLevel { get; init; }
    public required string Message { get; init; }
    public string? Trace { get; init; }
}

public record WhisperIpcRecognition
{
    public const char IDENTIFIER = 'R';
    public required string Text { get; init; }
    public required uint Id { get; init; }
    public required uint SubId { get; init; }
    public bool IsFinal { get; init; }
}

public record WhisperIpcKeepalive
{
    public const char IDENTIFIER = 'K';
    public required uint Index { get; init; }
}

public record WhisperIpcMute
{
    public const char IDENTIFIER = 'M';
    public required bool State { get; init; }
}

public record WhisperIpcStatus
{
    public const char IDENTIFIER = 'S';
    public required bool State { get; init; }
}