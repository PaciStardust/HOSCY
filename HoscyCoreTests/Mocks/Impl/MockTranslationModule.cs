using HoscyCore.Services.Translation.Core;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockTranslationModuleStartInfo : MockSoloModuleStartInfoBase, ITranslationModuleStartInfo
{
    public TranslationModuleConfigFlags ConfigFlags
        => TranslationModuleConfigFlags.None;
}


public abstract class MockTranslationModule : MockStartStopModuleBase, ITranslationModule
{
    public TranslationResult ReturnedResult { get; set; } = TranslationResult.Succeeded;
    public string? ReturnedOutput { get; set; } = null;

    public List<string> ReceivedInput { get; init; } = [];

    public TranslationResult TryTranslate(string input, out string? output)
    {
        ReceivedInput.Add(input);
        output = ReturnedOutput;
        return ReturnedResult;
    }

    public override void ResetStats()
    {
        base.ResetStats();
        ReceivedInput.Clear();
        ReturnedResult = TranslationResult.Succeeded;
        ReturnedOutput = null;
    }
}

public class MockTranslationModuleA : MockTranslationModule;
public class MockTranslationModuleB : MockTranslationModule;