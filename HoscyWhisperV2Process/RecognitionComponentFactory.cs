using HoscyCore.Services.Audio;
using HoscyCore.Services.Recognition.Extra;
using Serilog;
using SoundFlow.Abstracts;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;
using WebRtcVadSharp;
using Whisper.net;

namespace HoscyWhisperV2Process;

public class RecognitionComponentFactory(WhisperIpcConfig config)
{
    private readonly WhisperIpcConfig _config = config;

    public ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();
    }

    public AudioEngine CreateAudioEngine(ILogger logger)
    {
        logger.Debug("Starting audio engine");

        var engine = new MiniAudioEngine();
        engine.UpdateAudioDevicesInfo();

        return engine;
    }

    public AudioCaptureDeviceProxy CreateCaptureDevice(AudioEngine engine, ILogger logger)
    {
        logger.Debug("Creating audio device");

        var devInfo = AudioUtils.FindDevice(engine.CaptureDevices, _config.CaptureDeviceName, logger) 
            ?? throw new ArgumentException("Failed to locate a suitable microphone");

        var format = new AudioFormat()
        {
            Channels = 1,
            Format = SampleFormat.S16,
            Layout = ChannelLayout.Mono,
            SampleRate = 16_000
        };

        var rawDevice = engine.InitializeCaptureDevice(devInfo, format);
        return new(rawDevice, logger);
    }

    public WebRtcVad CreateVad(ILogger logger)
    {
        logger.Debug("Creating VAD");

        return new WebRtcVad()
        {
            OperatingMode = _config.VadOperatingMode switch
            {
                WhisperIpcVadOperatingMode.HighQuality => OperatingMode.HighQuality,
                WhisperIpcVadOperatingMode.LowBitrate => OperatingMode.LowBitrate,
                WhisperIpcVadOperatingMode.Aggressive => OperatingMode.Aggressive,
                WhisperIpcVadOperatingMode.VeryAggressive => OperatingMode.VeryAggressive,
                _ => OperatingMode.Aggressive
            },
            SampleRate = SampleRate.Is16kHz,
            FrameLength = FrameLength.Is10ms
        };
    }

    public AudioProcessor CreateAudioProcessor(WebRtcVad vad)
    {
        return new AudioProcessor(vad, _config);
    } 

    public WhisperProcessor CreateWhisperProcessor(ILogger logger)
    {
        logger.Debug("Creating whisper processor");

        var factoryOptions = new WhisperFactoryOptions();

        if (_config.Whisper_UseGpu)
        {
            factoryOptions.UseGpu = true;
            if (_config.Whisper_GpuId != 0)
            {
                factoryOptions.GpuDevice = _config.Whisper_GpuId;
            }
        }
        else
        {
            factoryOptions.UseGpu = false;
        }

        var whisperFactory = WhisperFactory.FromPath(_config.Whisper_ModelPath, factoryOptions);
        var whisperBuilder = whisperFactory.CreateBuilder();

        var whisperLanguages = WhisperFactory.GetSupportedLanguages().Select(x => x.ToLower()).ToArray();

        if (_config.Whisper_DetectLanguage)
        {
            whisperBuilder.WithLanguageDetection();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(_config.Whisper_Language))
            {
                var lowerLanguage = _config.Whisper_Language.ToLower();
                if (!whisperLanguages.Contains(lowerLanguage))
                {
                    logger.Warning("Language is invalid, possible languages: {languages}",
                        string.Join(", ", whisperLanguages));
                    whisperBuilder.WithLanguage("auto");
                }
                else
                {
                    whisperBuilder.WithLanguage(lowerLanguage);
                }
            }
            else
            {
                whisperBuilder.WithLanguage("auto");
            }
        }

        if (_config.Whisper_TranslateToEnglish)
        {
            whisperBuilder.WithTranslate();
        }

        if (!string.IsNullOrWhiteSpace(_config.Whisper_Prompt))
        {
            whisperBuilder.WithPrompt(_config.Whisper_Prompt);
        } 

        if (_config.Whisper_SingleSegment)
        {
            whisperBuilder.WithSingleSegment();
        }

        if (_config.Whisper_NoSpeechThreshold >= 0)
        {
            whisperBuilder.WithNoSpeechThreshold(Math.Min(_config.Whisper_NoSpeechThreshold, 1));
        }

        if (_config.Whisper_Temperature >= 0)
        {
            whisperBuilder.WithTemperature(Math.Min(_config.Whisper_Temperature, 1));
        }

        if (_config.Whisper_TemperatureInc >= 0)
        {
            whisperBuilder.WithTemperatureInc(Math.Min(_config.Whisper_TemperatureInc, 1));
        }

        if (_config.Whisper_MaxInitialT >= 0)
        {
            whisperBuilder.WithMaxInitialTs(Math.Min(_config.Whisper_MaxInitialT, 1));
        }

        if (_config.Whisper_SetThreads)
        {
            var maxThreads = Environment.ProcessorCount;
            var threadCount = _config.Whisper_ThreadCount switch
            {
                > 0 => Math.Min(_config.Whisper_ThreadCount, maxThreads),
                0 => maxThreads,
                < 0 => Math.Max(maxThreads + _config.Whisper_ThreadCount, 1)
            };
            whisperBuilder.WithThreads(threadCount);
        }

        if (_config.Whisper_MaxSegmentLength > 0)
        {
            whisperBuilder.WithMaxSegmentLength(_config.Whisper_MaxSegmentLength);
        }

        if (_config.Whisper_MaxTokensPerSegment > 0)
        {
            whisperBuilder.WithMaxTokensPerSegment(_config.Whisper_MaxTokensPerSegment);
        }

        if (_config.Whisper_UseGreedySampling)
        {
            whisperBuilder.WithGreedySamplingStrategy((x) =>
            {
                if (_config.Whisper_GreedyBestOf > 0)
                {
                    x.WithBestOf(_config.Whisper_GreedyBestOf);
                }
            });
        } 
        if (_config.Whisper_UseBeamSearchSampling)
        {
            whisperBuilder.WithBeamSearchSamplingStrategy((x) =>
            {
                if (_config.Whisper_BeamSize > 0)
                {
                    x.WithBeamSize(_config.Whisper_BeamSize);
                }
            });
        }
    
        return whisperBuilder.Build();
    }
}