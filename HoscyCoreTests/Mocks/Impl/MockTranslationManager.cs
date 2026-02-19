using HoscyCore.Services.Core;
using HoscyCore.Services.Translation.Core;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockTranslationManager : MockSoloModuleManagerBase<ITranslationModuleStartInfo> ,ITranslationManagerService
{
    public List<string> ReceivedInput { get; private init; } = [];

    public string? TranslateOutput { get; set; } = null;
    public TranslationResult TranslateResult { get; set; } = TranslationResult.Succeeded;
    public TranslationResult TryTranslate(string input, out string? output)
    {
        ReceivedInput.Add(input);
        output = TranslateOutput;
        return TranslateResult;
    }
}