using HoscyCore.Services.Recognition.Extra;
using Whisper.net;

namespace HoscyWhisperV2Process;

public class Program
{   
    public static async Task Main(string[] args)
    {
        /*
        Desired flow of operations:
        1. [ ] Reading of configuration variables from args
        2. [ ] Setting up IPC and phoning home
        3. [ ] Initialization of all required systems
        4. [ ] Starting of main flow using CT
        5. [ ] Listening to mute and stop events
        6. [ ] Shutdown on CT
        */

        var config = new WhisperIpcConfig()
        {
            Whisper_ModelPath = Console.ReadLine()!
        };

        var factory = new RecognitionComponentFactory(config);
        var logger = factory.CreateLogger();
        using var audioEngine = factory.CreateAudioEngine(logger);
        using var capture = factory.CreateCaptureDevice(audioEngine, logger);
        using var vad = factory.CreateVad(logger);
        var audioProcessor = factory.CreateAudioProcessor(vad);
        using var whisperProcessor = factory.CreateWhisperProcessor(logger);

        using var recCore = new WhisperRecognitionCore(whisperProcessor, audioProcessor, capture, logger);

        using var cts = new CancellationTokenSource();
        var task = Task.Run(async () => await recCore.RecognizeAsync(cts.Token, HandleRecognitionOutput));

        var wait = 250 / 5;
        while(!recCore.IsRunning && !cts.IsCancellationRequested && !task.IsCompleted && wait-- > 0)
        {
            await Task.Delay(5);
        }

        if (recCore.IsRunning && !cts.IsCancellationRequested && !task.IsCompleted)
        {
            capture.SetListening(true);
            Console.ReadLine();
            capture.SetListening(false);
        }
        else
        {
            logger.Error("Recognition task is unexpectedly not running");
        }

        cts.Cancel();
        await task;

        if (task.Exception is not null)
        {
            logger.Error(task.Exception, "Listening task encountered an error");
        }
    }

    private static void HandleRecognitionOutput(uint id, SegmentData data)
    {
        Console.WriteLine($"{id}: {data.Text}");
    }
}