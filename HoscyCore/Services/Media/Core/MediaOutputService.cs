using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Media.Core;

[PrototypeLoadIntoDiContainer(typeof(MediaOutputService))]
public class MediaOutputService(IMediaControlService control, IOutputManagerService output, ConfigModel config, ILogger logger)
    : StartStopServiceBase(logger.ForContext<MediaOutputService>()), IAutoStartStopService
{
    #region Injected
    private readonly IMediaControlService _control = control;
    private readonly IOutputManagerService _output = output;
    private readonly ConfigModel _config = config;
    #endregion

    #region Vars
    private bool _started = false;

    private bool _lastHandledPlaying = false;
    private MediaUpdateInfoTrack _lastHandledTrack = new();
    #endregion

    #region Start / Stop
    protected override bool IsStarted() => _started;
    protected override bool IsProcessing() => IsStarted();

    protected override Res StartForService()
    {
        _control.OnMediaUpdate += HandleMediaUpdate;
        _started = true;
        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => true;

    protected override Res StopForService()
    {
        _control.OnMediaUpdate -= HandleMediaUpdate;
        _started = false;
        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _lastHandledPlaying = false;
        _lastHandledTrack = new();
    }
    #endregion

    #region Event Handling
    private readonly OutputSettingsFlags _notificationFlags = OutputSettingsFlags.AllowTextOutput | OutputSettingsFlags.AllowOtherOutput;
    private readonly OutputNotificationPriority _notificationPriority = OutputNotificationPriority.Low;
    private void HandleMediaUpdate(MediaUpdateInfo info)
    {

        var currentPlaying = info.Playing ?? _lastHandledPlaying;
        if (!currentPlaying)
        {
            if (_lastHandledPlaying == currentPlaying)
                return;

            _lastHandledPlaying = false;

            if (!string.IsNullOrWhiteSpace(_config.Media_PauseText) && _config.Media_ShowStatus)
            {
                _logger.Information("Currently playing media has changed to PAUSED");
                _output.SendNotification(_config.Media_PauseText, _notificationPriority, _notificationFlags);
            }
            return;
        }

        _lastHandledPlaying = currentPlaying;

        if (!_config.Media_ShowStatus) return;

        MediaUpdateInfoTrack? cleanedTrack;        
        if (info.Track is null)
        {
            if (_lastHandledTrack is null) return;
            cleanedTrack = _lastHandledTrack;
        }
        else
        {
            cleanedTrack = CleanTrack(info.Track);
            if (cleanedTrack is null) return;

            if (cleanedTrack.Title.Equals(_lastHandledTrack.Title, StringComparison.OrdinalIgnoreCase)
                && cleanedTrack.Album.Equals(_lastHandledTrack.Album, StringComparison.OrdinalIgnoreCase)
                && cleanedTrack.Artists.Length == _lastHandledTrack.Artists.Length)
            {
                var allMatch = true;
                for (var i = 0; i < cleanedTrack.Artists.Length; i++)
                {
                    if (!cleanedTrack.Artists[i].Equals(_lastHandledTrack.Artists[i], StringComparison.OrdinalIgnoreCase))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch) return;
            }
        }

        _lastHandledTrack = cleanedTrack;
        var mediaString = CreateMediaString(cleanedTrack);

        foreach (var filter in _config.Media_Filters) {
            if (filter.Enabled && filter.Matches(mediaString))
            {
                return;
            }
        }

        _logger.Information("Currently playing media has changed to: {playing}", mediaString);
        _output.SendNotification($"{_config.Media_PlayingVerb} {mediaString}", _notificationPriority, _notificationFlags);
    }

    private MediaUpdateInfoTrack? CleanTrack(MediaUpdateInfoTrack? track)
    {
        if (track is null) return null;

        var trimTitle = track.Title.Trim();

        if (string.IsNullOrWhiteSpace(trimTitle))
            return null;

        var trimAlbum = track.Album.Trim();
        var trimArtists = track.Artists.Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        var newTrack = new MediaUpdateInfoTrack()
        {
            Album = trimTitle.Contains(trimAlbum, StringComparison.OrdinalIgnoreCase) && _config.Media_FilterSameNameAlbum
                ? string.Empty : trimAlbum,
            Title = trimTitle,
            Artists = trimArtists
        };

        return newTrack;
    }

    private string CreateMediaString(MediaUpdateInfoTrack track)
    {
        StringBuilder sb = new();

        if (track.Artists.Length == 0)
            sb.Append($"'{track.Title}'");
        else
        {
            var artistText = string.Join(", ", track.Artists);
            string appendText = _config.Media_SwapArtistAndSongInText
                ? $"'{artistText}' {_config.Media_IntermediateWord} '{track.Title}'"
                : $"'{track.Title}' {_config.Media_IntermediateWord} '{artistText}'";
            sb.Append(appendText);
        }

        if (_config.Media_AddAlbumToText && !string.IsNullOrWhiteSpace(track.Album))
            sb.Append($" {_config.Media_AlbumWord} '{track.Album}'");

        if (!string.IsNullOrWhiteSpace(_config.Media_ExtraText))
            sb.Append($" {_config.Media_ExtraText}");

        return sb.ToString();
    }
    #endregion
}