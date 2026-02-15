using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Translation.Core;

namespace HoscyCoreTests.Mocks;

public class MockTranslatorManager : MockStartStopServiceBase, ITranslatorManagerService
{
    public List<string> ReceivedInput { get; private init; } = [];

    public ServiceStatus CurrentProviderStatus { get; set; } = ServiceStatus.Processing;
    public ServiceStatus GetCurrentProviderStatus()
    {
        return CurrentProviderStatus;
    }

    public ITranslationProviderStartInfo? GetCurrentProviderInfo()
    {
        return null;
    }

    public IReadOnlyList<ITranslationProviderStartInfo> GetProviderInfos()
    {
        return [];
    }

    public void RefreshProvider()
    {
        return;
    }
    
    public bool RestartCurrentProvider()
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