using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockRecognitionManagerService : MockSoloModuleManagerBase<IRecognitionModuleStartInfo>, IRecognitionManagerService
{
    public bool IsListening => _listening;
    private bool _listening = false;

    public event EventHandler<RecognitionStatusChangedEventArgs> OnModuleStatusChanged = delegate { };
    public Res<bool> SetListening(bool state)
    {
        _listening = state;
        return ResC.TOk(state);
    }
    public Res UpdateSettings()
    {
        return ResC.Ok();
    }
}