using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using HoscyCore.Services.DependencyCore;
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

    public bool ShouldContinueIfHandled()
        => true;

    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        if (!input.StartsWith(COMMAND_PREFIX, System.StringComparison.OrdinalIgnoreCase))
        {
            output = null;
            return false;
        }

        _logger.Debug("File command detected \"{fileCommand}\", attempting to load", input);
        var filePath = _commandPrefixRemover.Replace(input, string.Empty);
        try
        {
            var lines = File.ReadLines(filePath);
            _logger.Debug("Successfully loaded contents of file from path \"{filePath}\"", filePath);
            output = string.Join("\n", lines);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unable to load file from path \"{filePath}\"", filePath);
            output = "[File Read Error]";
            return true;
        }
    }
}