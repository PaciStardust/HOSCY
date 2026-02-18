using HoscyCore.Services.Core;

namespace HoscyCore.Services.Recognition.Core;

public interface IRecognitionModuleStartInfo : ISoloModuleStartInfo;

public interface IRecognitionModule : IStartStopModule
{
    public event Action<string> OnSpeechRecognized;
    public event Action<bool> OnSpeechActivity;

    public bool IsListening { get; }
    public bool SetListening(bool state);
}