using HoscyCore.Services.Recognition.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockRecognitionModuleStartInfo : MockSoloModuleStartInfoBase, IRecognitionModuleStartInfo
{
    public RecognitionModuleConfigFlags ConfigFlags => RecognitionModuleConfigFlags.None;
}


public abstract class MockRecognitionModule : MockStartStopModuleBase, IRecognitionModule
{
    private bool _listening = false;
    public bool IsListening => _listening;

    public void InvokeSpeechRecognized(string message)
        => OnSpeechRecognized(message);
    public event Action<string> OnSpeechRecognized = delegate { };

    public void InvokeSpeechActivity(bool state)
        => OnSpeechActivity.Invoke(state);
    public event Action<bool> OnSpeechActivity = delegate { };

    public void InvokeInternalListeningStatusChange()
        => OnInternalListeningStatusChange();
    public event Action OnInternalListeningStatusChange = delegate { };

    public bool FailOnSetListening { get; set; } = false;
    public Res<bool> SetListening(bool state)
    {
        if (FailOnSetListening) return ResC.TFail<bool>("Oops!");
        _listening = state;
        return ResC.TOk(state);
    }

    public override void ResetStats()
    {
        base.ResetStats();
        FailOnSetListening = false;
    }
}

public class MockRecognitionModuleA : MockRecognitionModule;
public class MockRecognitionModuleB : MockRecognitionModule;