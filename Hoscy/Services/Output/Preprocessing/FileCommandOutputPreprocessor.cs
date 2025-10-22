using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Preprocessing;

/// <summary>
/// Handles reading from files as a command
/// </summary>
[LoadIntoDiContainer(typeof(FileCommandOutputPreprocessor), Lifetime.Singleton)]
public partial class FileCommandOutputPreprocessor(ILogger logger) : IOutputPreprocessor
{
    private readonly ILogger _logger = logger.ForContext<FileCommandOutputPreprocessor>();

    private const string COMMAND_PREFIX = "[file]";
    [GeneratedRegex(@$"{COMMAND_PREFIX} *", RegexOptions.IgnoreCase)]
    private static partial Regex CommandPrefixRemover();

    public OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.ReplaceLate;

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
        var filePath = CommandPrefixRemover().Replace(input, string.Empty);
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
            output = null;
            return false;
        }
    }
}