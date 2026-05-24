using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

[PrototypeLoadIntoDiContainer(typeof(MediaCommandOutputPreprocessor))] //todo: [TEST]
public class MediaCommandOutputPreprocessor(IMediaControlService media, ILogger logger) : IOutputPreprocessor
{
    private readonly IMediaControlService _media = media;
    private readonly ILogger _logger = logger.ForContext<MediaCommandOutputPreprocessor>();

    public OutputPreprocessorHandlingStage GetHandlingStage()
        => OutputPreprocessorHandlingStage.Final;
    public bool IsEnabled() => true; //todo: [FIX] Should not be methods?
    public bool IsFullReplace() => true;

    private const string COMMAND_KEYWORD = "[media]";
    public OutputPreprocessorResult Process(ref string contents)
    {
        if (!contents.StartsWith(COMMAND_KEYWORD, StringComparison.OrdinalIgnoreCase))
            return OutputPreprocessorResult.NotProcessed;

        var command = contents.ToLower().Replace(COMMAND_KEYWORD, string.Empty).Trim();

        Task<Res>? task = command switch
        {
            "pause" or "stop" or "cancel" => _media.PauseAsync(),
            "resume" or "start" or "play" => _media.PlayAsync(),
            "toggle" or "play pause" or "playpause" => _media.PlayPauseAsync(),
            "next" or "skip" or "forward" => _media.NextAsync(),
            "previous" or "last" => _media.PreviousAsync(),
            _ => null
        };

        task?.RunWithoutAwait();
        return OutputPreprocessorResult.ProcessedStop;
    }
}