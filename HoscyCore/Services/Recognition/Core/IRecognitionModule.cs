using HoscyCore.Services.Core;

namespace HoscyCore.Services.Recognition.Core;

public interface IRecognitionModuleStartInfo : ISoloModuleStartInfo;

public interface IRecognitionModule : IStartStopModule
{
    public Action<string> OnSpeechRecognized();
    public Action<bool> OnSpeechActivity();
    public EventHandler<RecognitionStatusChangedEventArgs> OnModuleStatusChanged(); //todo: [FIX] Move to manager

    public bool IsListening { get; }
    public bool SetListening(bool state);
}