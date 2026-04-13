using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Recognition.Core;

public interface IRecognitionManagerService : ISoloModuleManager<IRecognitionModuleStartInfo>
{
    public event EventHandler<RecognitionStatusChangedEventArgs> OnModuleStatusChanged;

    public bool IsListening { get; }
    public Res<bool> SetListening(bool state);

    public Res UpdateSettings();
}