using HoscyCore.Services.Core;

namespace HoscyCore.Services.Recognition.Core;

public interface IRecognitionManagerService : IStartStopModuleController<IRecognitionModuleStartInfo, IRecognitionModule>
{
    public EventHandler<RecognitionStatusChangedEventArgs> OnModuleStatusChanged();

    public bool IsListening { get; }
    public bool SetListening(bool state);
}