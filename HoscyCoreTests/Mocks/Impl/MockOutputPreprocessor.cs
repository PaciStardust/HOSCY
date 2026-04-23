using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.Output.Core;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOutputPreprocessor : IOutputPreprocessor
{
    public required OutputPreprocessorHandlingStage HandlingStage { get; set; }
    public required bool FullReplace { get; set; }
    public required bool ContinueIfHandled { get; set; }

    public bool Enabled { get; set; } = false;
    public string? ProcessedOutput { get; set; } = null;
    public bool OutputAfterStop { get; set; } = false;
    public List<string> ReceivedInput { get; init; } = [];

    public OutputPreprocessorHandlingStage GetHandlingStage()
        => HandlingStage;

    public bool IsEnabled()
        => Enabled;

    public bool IsFullReplace()
        => FullReplace;

    public OutputPreprocessorResult Process(ref string contents)
    {
        ReceivedInput.Add(contents);

        if (ProcessedOutput is not null)
        {
            contents = ProcessedOutput;
            return ContinueIfHandled 
                ? OutputPreprocessorResult.ProcessedContinue 
                : OutputAfterStop
                    ? OutputPreprocessorResult.ProcessedStopOutput
                    : OutputPreprocessorResult.ProcessedStop;
        }
        
        return OutputPreprocessorResult.NotProcessed;
    }
}