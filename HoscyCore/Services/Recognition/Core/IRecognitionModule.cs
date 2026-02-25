using HoscyCore.Services.Core;

namespace HoscyCore.Services.Recognition.Core;

public interface IRecognitionModuleStartInfo : ISoloModuleStartInfo
{
    public RecognitionModuleConfigFlags ConfigFlags { get; }
}

[Flags]
public enum RecognitionModuleConfigFlags
{
    None = 0b0,
    Microphone = 0b1
}

public interface IRecognitionModule : IStartStopModule
{
    public event Action<string> OnSpeechRecognized;
    public event Action<bool> OnSpeechActivity;

    public bool IsListening { get; }
    public bool SetListening(bool state);
}