using System.Diagnostics.CodeAnalysis;

namespace Hoscy.Services.Output.Core;

public interface IOutputPreprocessor
{
    public OutputPreprocessorHandlingStage GetHandlingStage();

    /// <summary>
    /// Indicates if processing should continue if processing successful
    /// </summary>
    public bool ShouldContinueIfHandled();

    /// <summary>
    /// Tries processing the input
    /// </summary>
    /// <param name="input">Input to process</param>
    /// <param name="output">Output of processing if returning true</param>
    /// <returns>Success</returns>
    public bool TryProcess(string input, [NotNullWhen(true)] out string? output);
}