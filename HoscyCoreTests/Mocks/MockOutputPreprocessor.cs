using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks;

public class MockOutputPreprocessor : IOutputPreprocessor
{
    public required OutputPreprocessorHandlingStage HandlingStage { get; set; }
    public required bool FullReplace { get; set; }
    public required bool ContinueIfHandled { get; set; }

    public bool Enabled { get; set; } = false;
    public string? ProcessedOutput { get; set; } = null;
    public List<string> ReceivedInput { get; init; } = [];

    public OutputPreprocessorHandlingStage GetHandlingStage()
        => HandlingStage;

    public bool IsEnabled()
        => Enabled;

    public bool IsFullReplace()
        => FullReplace;

    public bool ShouldContinueIfHandled()
        => ContinueIfHandled;

    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        ReceivedInput.Add(input);

        if (ProcessedOutput is not null)
        {
            output = ProcessedOutput;
            return true;
        }
        
        output = null;
        return false;
    }
}