using Hoscy.Services.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Control;

namespace Hoscy.Services.Api
{
    public static class Media
    {
        private static GlobalSystemMediaTransportControlsSessionManager? _gsmtcsm;
        private static GlobalSystemMediaTransportControlsSession? _session;
        private static GlobalSystemMediaTransportControlsSessionMediaProperties? _nowPlaying;

        #region Startup
        public static void StartMediaDetection()
            => App.RunWithoutAwait(StartMediaDetectionInternal());

        private static async Task StartMediaDetectionInternal()
        {
            Logger.PInfo("Started media detection service");

            _gsmtcsm = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _gsmtcsm.CurrentSessionChanged += Gsmtcsm_CurrentSessionChanged;

            GetCurrentSession(_gsmtcsm);
        }

        private static void GetCurrentSession(GlobalSystemMediaTransportControlsSessionManager sender)
        {
            _session = sender.GetCurrentSession();
            _nowPlaying = null;

            Logger.Info($"Media session has been changed to {_session?.SourceAppUserModelId ?? "None"}");

            if (_session == null)
                return;

            _session.MediaPropertiesChanged += Session_MediaPropertiesChanged;
            _session.PlaybackInfoChanged += Session_PlaybackInfoChanged;

            UpdateCurrentlyPlayingMediaProxy(_session);
        }
        #endregion

        #region External Connection
        private readonly static object _lock = new();
        private async static Task UpdateCurrentlyPlayingMedia(GlobalSystemMediaTransportControlsSession sender)
        {
            var newPlaying = await sender.TryGetMediaPropertiesAsync();
            var playbackInfo = sender.GetPlaybackInfo();

            //Set notification empty if the current media is invalid
            if (newPlaying == null //No new playing
                || !(newPlaying.PlaybackType == MediaPlaybackType.Video || newPlaying.PlaybackType == MediaPlaybackType.Music) //Not a video or music
                || playbackInfo == null || playbackInfo.PlaybackStatus!= GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing //No status or its not playing
                || newPlaying.Title == null || string.IsNullOrWhiteSpace(newPlaying.Title)) //No title
            {
                SetNotification(string.Empty);
                return;
            }

            //Checking if the new media is the same media
            //Locked as it sometimes happens to come here multiple times concurrently
            lock (_lock)
            {
                if (_nowPlaying != null) //We skip this check if there is now now playing
                    if (newPlaying.Title != _nowPlaying.Title && newPlaying.Artist == _nowPlaying.Artist)
                        return;
            }

            _nowPlaying = newPlaying;

            if (Config.Textbox.MediaShowStatus)
            {
                var playing = CreateCurrentMediaString();

                if (string.IsNullOrWhiteSpace(playing))
                {
                    SetNotification(string.Empty);
                    return;
                }

                Logger.Log($"Currently playing media has changed to: {playing}");
                SetNotification($"{Config.Textbox.MediaPlayingVerb} {playing}");
            }
        }

        private static string? CreateCurrentMediaString()
        {
            if (_nowPlaying == null || string.IsNullOrWhiteSpace(_nowPlaying.Title)) //This should in theory never trigger but just to be sure
                return null;

            StringBuilder sb = new($"'{_nowPlaying.Title}'");

            if (!string.IsNullOrWhiteSpace(_nowPlaying.Artist))
                sb.Append($" by '{_nowPlaying.Artist}'");

            if (Config.Textbox.MediaAddAlbum && !string.IsNullOrWhiteSpace(_nowPlaying.AlbumTitle) && _nowPlaying.AlbumTitle != _nowPlaying.Title)
                sb.Append($" on '{_nowPlaying.AlbumTitle}'");

            return sb.ToString();
        }

        private static string? CreateDetailedMediaString()
        {
            StringBuilder sb = new(CreateCurrentMediaString());

            if (sb.Length == 0 || _nowPlaying == null)
                return null;

            if (!Config.Textbox.MediaAddAlbum && !string.IsNullOrWhiteSpace(_nowPlaying.AlbumTitle))
                sb.Append($"on '{_nowPlaying.AlbumTitle}'");

            if (_nowPlaying.Genres.Count > 0)
                sb.Append($" ({string.Join(", ", _nowPlaying.Genres)})");

            return sb.ToString();
        }

        private static void SetNotification(string text)
            => Textbox.Notify(text, NotificationType.Media);
        #endregion

        #region Events
        /// <summary>
        /// Triggers whenever is paused, skipped, etc
        /// </summary>
        private static void Session_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
            => UpdateCurrentlyPlayingMediaProxy(sender);

        /// <summary>
        /// Triggers on song switch
        /// </summary>
        private static void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
            => UpdateCurrentlyPlayingMediaProxy(sender);

        /// <summary>
        /// Triggers when a new session is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Gsmtcsm_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
            => GetCurrentSession(sender);

        private static void UpdateCurrentlyPlayingMediaProxy(GlobalSystemMediaTransportControlsSession sender)
            => App.RunWithoutAwait(UpdateCurrentlyPlayingMedia(sender));
        #endregion

        #region Media Control
        /// <summary>
        /// Enum to classify the different commands that can be executed
        /// </summary>
        private enum MediaCommandType
        {
            None,
            Pause,
            Unpause,
            Rewind,
            Skip,
            Info
        }

        /// <summary>
        /// List of command aliases
        /// </summary>
        private static readonly IReadOnlyDictionary<string, MediaCommandType> _commandTriggers = new Dictionary<string, MediaCommandType>()
        {
            { "pause", MediaCommandType.Pause },
            { "stop", MediaCommandType.Pause },

            { "resume", MediaCommandType.Unpause },
            { "play", MediaCommandType.Unpause },

            { "skip", MediaCommandType.Skip },
            { "next", MediaCommandType.Skip },

            { "rewind", MediaCommandType.Rewind },
            { "back", MediaCommandType.Rewind },

            { "info", MediaCommandType.Info },
            { "current", MediaCommandType.Info },
            { "status", MediaCommandType.Info },
            { "now", MediaCommandType.Info }
        };

        /// <summary>
        /// Handles the raw media command, skips otherwise
        /// </summary>
        /// <param name="command">Raw command</param>
        public static void HandleRawMediaCommand(string command)
        {
            if (!_commandTriggers.Keys.Contains(command))
                return;

            var mediaCommand = _commandTriggers[command];
            App.RunWithoutAwait(HandleMediaCommand(mediaCommand));
        }

        /// <summary>
        /// Actual handling of media commands
        /// </summary>
        /// <param name="command">command type</param>
        private async static Task HandleMediaCommand(MediaCommandType command)
        {
            if (_session == null)
                return;

            switch (command)
            {
                case MediaCommandType.Pause:
                    if (await _session.TryPauseAsync())
                        Logger.Log("Paused media playback");
                    return;

                case MediaCommandType.Unpause:
                    if (await _session.TryPlayAsync())
                        Logger.Log("Resumed media playback");
                    return;

                case MediaCommandType.Skip:
                    if (await _session.TrySkipNextAsync())
                        Logger.Log("Skipped media playback");
                    return;

                case MediaCommandType.Rewind:
                    if (await _session.TrySkipPreviousAsync())
                        Logger.Log("Rewinded media playback");
                    return;

                case MediaCommandType.Info:
                    var playing = CreateDetailedMediaString();
                    if (string.IsNullOrWhiteSpace(playing))
                        return;
                    SetNotification(playing);
                    return;

                default: return;
            }
        }
        #endregion
    }
}
