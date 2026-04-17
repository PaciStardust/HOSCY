using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Recognition.Core;

public interface IRecognitionModuleStartInfo : ISoloModuleStartInfo
{
    public RecognitionModuleConfigFlags ConfigFlags { get; }
}

[Flags]
public enum RecognitionModuleConfigFlags
{
    None = 0b0,
    Microphone = 0b1,
    Windows = 0b10,
    AnyApi = 0b100,
    Whisper = 0b1000
}

public interface IRecognitionModule : IStartStopModule
{
    public event Action<string> OnSpeechRecognized;
    public event Action<bool> OnSpeechActivity;
    public event Action OnInternalListeningStatusChange;

    public bool IsListening { get; }
    
    /// <summary>
    /// Sets the listening status of the module
    /// </summary>
    /// <param name="state">Target state</param>
    /// <returns>Result state</returns>
    public Res<bool> SetListening(bool state);
}