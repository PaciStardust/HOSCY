using HoscyCore.Services.Dependency;
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

    public OutputPreprocessorResult Process(ref string contents)
    {
        if (!_cmd.DetectCommand(contents))
            return OutputPreprocessorResult.NotProcessed;

        _logger.Debug("Detected command \"{command}\", forwarding to service", contents);
        var result = _cmd.HandleCommand(contents);
        // output = $"Command => {(result.IsOk ? result.Value : $"Fail ({result.Msg})")}";
        return OutputPreprocessorResult.ProcessedStop;
    }
}