using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

/// <summary>
/// Handles reading from files as a command
/// </summary>
[LoadIntoDiContainer(typeof(FileCommandOutputPreprocessor), Lifetime.Transient)]
public partial class FileCommandOutputPreprocessor(ILogger logger) : IOutputPreprocessor
{
    private readonly ILogger _logger = logger.ForContext<FileCommandOutputPreprocessor>();
    private const string COMMAND_PREFIX = "[file]";
    private static readonly Regex _commandPrefixRemover = new(@"\[FILE\] *", RegexOptions.IgnoreCase);

    public bool IsEnabled()
        => true;

    public OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.ReplaceLate;

    public bool IsFullReplace()
        => true;

    public OutputPreprocessorResult Process(ref string contents)
    {
        if (!contents.StartsWith(COMMAND_PREFIX, StringComparison.OrdinalIgnoreCase))
            return OutputPreprocessorResult.NotProcessed;

        _logger.Debug("File command detected \"{fileCommand}\", attempting to load", contents);
        var filePath = _commandPrefixRemover.Replace(contents, string.Empty);
        try
        {
            var lines = File.ReadLines(filePath);
            _logger.Debug("Successfully loaded contents of file from path \"{filePath}\"", filePath);
            contents = string.Join("\n", lines);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unable to load file from path \"{filePath}\"", filePath);
            contents = "[File Read Error]";
        }
        return OutputPreprocessorResult.ProcessedStopOutput;
    }
}