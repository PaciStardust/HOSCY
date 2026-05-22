using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Interfacing;
using HoscyCore.Services.Media.Core;
using HoscyCore.Services.Osc.MessageHandling.Core;
using HoscyCore.Utility;
using LucHeart.CoreOSC;
using Serilog;

namespace HoscyCore.Services.Osc.MessageHandling.Handlers;

[LoadIntoDiContainer(typeof(MediaOscMessageHandler))] //todo: [TEST]
public class MediaOscMessageHandler
(
    ConfigModel config,
    IMediaControlService media,
    IBackToFrontNotifyService notify,
    ILogger logger
) 
    : IOscMessageHandler
{
    private readonly ConfigModel _config = config;
    private readonly IMediaControlService _media = media;
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ILogger _logger = logger.ForContext<MediaOscMessageHandler>();

    public bool HandleMessage(OscMessage message)
    {
        if (message.Address.Equals(_config.Osc_Address_Media_Pause, StringComparison.OrdinalIgnoreCase))
        {
            _media.PauseAsync().ContinueWith((x, _) => OnMediaTaskComplete(x, "Pause"), TaskContinuationOptions.None)
                .RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Rewind, StringComparison.OrdinalIgnoreCase))
        {
            _media.PreviousAsync().ContinueWith((x, _) => OnMediaTaskComplete(x, "Previous"), TaskContinuationOptions.None)
                .RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Skip, StringComparison.OrdinalIgnoreCase))
        {
            _media.NextAsync().ContinueWith((x, _) => OnMediaTaskComplete(x, "Next"), TaskContinuationOptions.None)
                .RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Toggle, StringComparison.OrdinalIgnoreCase))
        {
            _media.PlayPauseAsync().ContinueWith((x, _) => OnMediaTaskComplete(x, "Toggle"), TaskContinuationOptions.None)
                .RunWithoutAwait();
            return true;
        }
        else if (message.Address.Equals(_config.Osc_Address_Media_Unpause, StringComparison.OrdinalIgnoreCase))
        {
            _media.PlayAsync().ContinueWith((x, _) => OnMediaTaskComplete(x, "Play"), TaskContinuationOptions.None)
                .RunWithoutAwait();
            return true;
        }
        return false;
    }

    private Task OnMediaTaskComplete(Task<Res> task, string actionForLog)
    {
        if (task.IsFaulted)
        {
            var msg = ResC.FailLog($"Failed to execute media command \"{actionForLog}\" for unknown reason", _logger, task.Exception);
            _notify.SendResult($"Media command \"{actionForLog}\" failed", msg.Msg!);
        }
        else if (task.IsCompletedSuccessfully && task.Result is not null && !task.Result.IsOk)
        {
            _logger.Warning($"Failed to execute media command \"{actionForLog}\" with result: {task.Result.Msg}");
            _notify.SendResult($"Media command \"{actionForLog}\" failed", task.Result.Msg);
        } 
        return Task.CompletedTask;
    }
}