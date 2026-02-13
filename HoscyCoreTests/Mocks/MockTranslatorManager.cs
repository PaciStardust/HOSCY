using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Translation.Core;

namespace HoscyCoreTests.Mocks;

public class MockTranslatorManager : MockStartStopServiceBase, ITranslatorManagerService
{
    public IReadOnlyList<(string ProperName, string TypeName)> GetAvailableNames()
    {
        return [ ("Mock", "Mock") ];
    }

    public string? GetCurrentName()
    {
        return "Mock";
    }

    public ServiceStatus CurrentTranslatorStatus { get; set; } = ServiceStatus.Processing;
    public ServiceStatus GetCurrentTranslatorStatus()
    {
        return CurrentTranslatorStatus;
    }

    public void RestartCurrentTranslator()
    {
        return;
    }

    public void StartTranslator(string? name = null, string? typeName = null)
    {
        return;
    }

    public void StopCurrentTranslator()
    {
        return;
    }

    public TranslationResult TranslateResult { get; set; } = TranslationResult.Succeeded;
    public string? TranslateOutput { get; set; } = null;
    public List<string> ReceivedInput { get; private init; } = [];
    public TranslationResult TryTranslate(string input, out string? output)
    {
        ReceivedInput.Add(input);
        output = TranslateOutput;
        return TranslateResult;
    }
}