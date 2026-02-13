using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Services.Osc.Command;
using HoscyCore.Services.Output.Core;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

/// <summary>
/// Preprocesssor for OSC Commands
/// </summary
[LoadIntoDiContainer(typeof(OscCommandOutputPreprocessor), Lifetime.Transient)]
public class OscCommandOutputPreprocessor(IOscCommandService cmd, ILogger logger) : IOutputPreprocessor
{
    private readonly IOscCommandService _cmd = cmd;
    private readonly ILogger _logger = logger.ForContext<OscCommandOutputPreprocessor>();

    public bool IsEnabled()
        => true;

    public OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Final;

    public bool IsFullReplace()
        => true;

    public bool ShouldContinueIfHandled()
        => false;

    public bool TryProcess(string input, [NotNullWhen(true)] out string? output)
    {
        if (!_cmd.DetectCommand(input))
        {
            output = null;
            return false;
        }

        _logger.Debug("Detected command \"{command}\", forwarding to service");
        var result = _cmd.HandleCommand(input);
        output = $"Command => {result}";
        return true;
    }
}