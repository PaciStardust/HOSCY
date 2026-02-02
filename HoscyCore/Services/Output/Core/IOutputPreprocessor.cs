using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;

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
    /// Indicates if processing should continue if processing successful
    /// </summary>
    public bool ShouldContinueIfHandled();

    /// <summary>
    /// Tries processing the input
    /// </summary>
    /// <param name="input">Input to process</param>
    /// <param name="output">Output of processing if returning true</param>
    public bool TryProcess(string input, [NotNullWhen(true)] out string? output);
}