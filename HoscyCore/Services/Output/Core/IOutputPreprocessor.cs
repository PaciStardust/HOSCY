using System.Text;
using HoscyCore.Services.Core;

namespace HoscyCore.Services.Output.Core;

public interface IOutputPreprocessor : IService
{
    /// <summary>
    /// Should preprocessor even be used?
    /// </summary>
    public bool IsEnabled();

    /// <summary>
    /// Retrieve the handling stage of the preprocessor
    /// </summary>
    public OutputPreprocessorHandlingStage GetHandlingStage();

    /// <summary>
    /// Indicates if text is fully replaced
    /// </summary>
    public bool IsFullReplace();

    /// <summary>
    /// Tries processing the input
    /// </summary>
    /// <param name="input">Input to process</param>
    /// <param name="output">Output of processing if returning true</param>
    public OutputPreprocessorResult Process(ref string contents);
}

public enum OutputPreprocessorResult
{
    NotProcessed = 0,
    ProcessedContinue = 1,
    ProcessedStop = 2,
    ProcessedStopOutput = 3
}