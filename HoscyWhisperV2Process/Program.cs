using System.Collections.Concurrent;
using System.Globalization;
using EchoSharp.Abstractions.SpeechTranscription;
using EchoSharp.SpeechTranscription;
using EchoSharp.WebRtc.WebRtcVadSharp;
using EchoSharp.Whisper.net;
using Serilog.Core;
using Serilog.Events;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Enums;
using SoundFlow.Structs;
using WebRtcVadSharp;
using Whisper.net;

namespace HoscyWhisperV2Process;

public class Program
{
    private static ConcurrentQueue<float[]> _toProcess = [];

    public static async Task Main(string[] args)
    {
        using var engine = new MiniAudioEngine();
        engine.UpdateAudioDevicesInfo();

        var info = engine.CaptureDevices.FirstOrDefault(x => x.IsDefault);
        using var capture = engine.InitializeCaptureDevice(info, new AudioFormat() { Channels=1, Format=SampleFormat.S16, Layout=ChannelLayout.Mono, SampleRate=16000});

        var audio = new HoscyAvailableAudioSource(capture);

        var vad = new WebRtcVadSharpDetectorFactory(new WebRtcVadSharpOptions()
        {
            OperatingMode = OperatingMode.VeryAggressive
        });

        var path = Console.ReadLine();
        var rec = new WhisperSpeechTranscriptorFactory(WhisperFactory.FromPath(path!));

        var trans = new EchoSharpRealtimeTranscriptorFactory(rec, vad, echoSharpOptions: new EchoSharpRealtimeOptions()
        {
            ConcatenateSegmentsToPrompt = false // Flag to concatenate segments to prompt when new segment is recognized (for the whole session)
        }).Create(new RealtimeSpeechTranscriptorOptions()
        {
            AutodetectLanguageOnce = false, // Flag to detect the language only once or for each segment
            IncludeSpeechRecogizingEvents = false, // Flag to include speech recognizing events (RealtimeSegmentRecognizing)
            RetrieveTokenDetails = true, // Flag to retrieve token details
            LanguageAutoDetect = false, // Flag to auto-detect the language
            Language = new CultureInfo("en-US"), // Language to use for transcription
        });

        var microphoneTask = Task.Run(() =>
        {
            audio.StartRecording();
            Console.WriteLine("Speak to recognize, press any key to stop...");
            Console.ReadKey();
            audio.StopRecording();
        });

        async Task ShowTranscriptAsync()
        {
            await foreach (var transcription in trans.TranscribeAsync(audio))
            {
                var eventType = transcription.GetType().Name;
                Console.WriteLine(eventType);

                var textToWrite = transcription switch
                {
                    RealtimeSegmentRecognized segmentRecognized => $"{segmentRecognized.Segment.StartTime}-{segmentRecognized.Segment.StartTime + segmentRecognized.Segment.Duration}:{segmentRecognized.Segment.Text}",
                    RealtimeSegmentRecognizing segmentRecognizing => $"{segmentRecognizing.Segment.StartTime}-{segmentRecognizing.Segment.StartTime + segmentRecognizing.Segment.Duration}:{segmentRecognizing.Segment.Text}",
                    RealtimeSessionStarted sessionStarted => $"SessionId: {sessionStarted.SessionId}",
                    RealtimeSessionStopped sessionStopped => $"SessionId: {sessionStopped.SessionId}",
                    _ => string.Empty
                };

                Console.WriteLine(textToWrite);
            }
        };

        var showTranscriptTask = ShowTranscriptAsync();
        var firstReady = await Task.WhenAny(microphoneTask, showTranscriptTask);

        // We await the task that finish first in case we have some exception to throw
        await firstReady;

        await Task.WhenAll(microphoneTask, showTranscriptTask);
    }

    private static void OnAudioProcessed(Span<float> samples, Capability capability)
    {
        _toProcess.Enqueue(samples.ToArray());
    }

    private static bool _stopped = false;
    private static async Task HandleAudioLoop(WhisperProcessor processor)
    {
        while (!_stopped)
        {
            if (_toProcess.IsEmpty || !_toProcess.TryDequeue(out var floats))
            {
                Thread.Sleep(10);
                continue;
            }

            await foreach (var segment in processor.ProcessAsync(floats))
            {
                Console.WriteLine(segment.Text);
            }
        }
    }
}

public class LogSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        Console.WriteLine($"{logEvent.Level} | {logEvent.MessageTemplate}");
    }
}