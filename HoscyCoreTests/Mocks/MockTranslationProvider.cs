using HoscyCore.Services.Translation.Core;

namespace HoscyCoreTests.Mocks;

public abstract class MockTranslationProvider : MockStartStopSubmoduleBase, ITranslationProvider
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

public class MockTranslationProviderA : MockTranslationProvider;
public class MockTranslationProviderB : MockTranslationProvider;