using HoscyCore.Services.Core;
using HoscyCore.Services.Translation.Core;

namespace HoscyCoreTests.Mocks;

public class MockTranslationManager : MockStartStopServiceBase, ITranslationManagerService
{
    public List<string> ReceivedInput { get; private init; } = [];

    public ServiceStatus CurrentModuleStatus { get; set; } = ServiceStatus.Processing;
    public ServiceStatus GetCurrentModuleStatus()
    {
        return CurrentModuleStatus;
    }

    public ITranslationModuleStartInfo? GetCurrentModuleInfo()
    {
        return null;
    }

    public IReadOnlyList<ITranslationModuleStartInfo> GetModuleInfos()
    {
        return [];
    }

    public void RefreshModuleSelection()
    {
        return;
    }
    
    public bool RestartCurrentModule()
    {
        return true;
    }

    public string? TranslateOutput { get; set; } = null;
    public TranslationResult TranslateResult { get; set; } = TranslationResult.Succeeded;
    public TranslationResult TryTranslate(string input, out string? output)
    {
        ReceivedInput.Add(input);
        output = TranslateOutput;
        return TranslateResult;
    }
}