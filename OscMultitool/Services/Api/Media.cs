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
        private static DateTime _mediaLastChanged =  DateTime.MinValue;

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
                    if (newPlaying.Title.Equals(_nowPlaying.Title, StringComparison.OrdinalIgnoreCase) && newPlaying.Artist.Equals(_nowPlaying.Artist) && (DateTime.Now - _mediaLastChanged).TotalSeconds < 5)
                        return;
            }

            _nowPlaying = newPlaying;
            _mediaLastChanged = DateTime.Now;

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
            Info,
            TogglePlayback
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
            { "now", MediaCommandType.Info },

            { "toggle", MediaCommandType.TogglePlayback },
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
        /// Handles osc media commands
        /// </summary>
        /// <param name="address">Osc Address</param>
        /// <returns>Was a command executed</returns>
        public static bool HandleOscMediaCommands(string address)
        {
            //I wish this could be a switch but those expect constants
            var command = MediaCommandType.None;

            if (address == Config.Osc.AddressMediaInfo)
                command = MediaCommandType.Info;
            else if (address == Config.Osc.AddressMediaToggle)
                command = MediaCommandType.TogglePlayback;
            else if (address == Config.Osc.AddressMediaPause)
                command = MediaCommandType.Pause;
            else if (address == Config.Osc.AddressMediaRewind)
                command = MediaCommandType.Rewind;
            else if (address == Config.Osc.AddressMediaSkip)
                command = MediaCommandType.Skip;
            else if (address == Config.Osc.AddressMediaUnpause)
                command = MediaCommandType.Unpause;

            App.RunWithoutAwait(HandleMediaCommand(command));
            return command != MediaCommandType.None;
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
                case MediaCommandType.TogglePlayback:
                    if (await _session.TryTogglePlayPauseAsync())
                        Logger.Log("Toggled media playback");
                    return;

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
                    var playing = CreateCurrentMediaString();
                    if (string.IsNullOrWhiteSpace(playing))
                        return;
                    SetNotification($"{Config.Textbox.MediaPlayingVerb} {playing}");
                    return;

                default: return;
            }
        }
        #endregion
    }
}
