using HoscyCore.Services.Translation.Core;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockTranslationModuleStartInfo : MockSoloModuleStartInfoBase, ITranslationModuleStartInfo
{
    public TranslationModuleConfigFlags ConfigFlags
        => TranslationModuleConfigFlags.None;
}


public abstract class MockTranslationModule : MockStartStopModuleBase, ITranslationModule
{
    public string? ReturnedOutput { get; set; } = null;

    public List<string> ReceivedInput { get; init; } = [];

    public Res<string> Translate(string input)
    {
        ReceivedInput.Add(input);
        return ReturnedOutput is null
            ? ResC.TFail<string>("No result")
            : ResC.TOk(ReturnedOutput);
    }

    public override void ResetStats()
    {
        base.ResetStats();
        ReceivedInput.Clear();
        ReturnedOutput = null;
        ResultToReturn = null;
    }
}

public class MockTranslationModuleA : MockTranslationModule;
public class MockTranslationModuleB : MockTranslationModule;