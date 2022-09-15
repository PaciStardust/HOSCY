using Hoscy;
using Hoscy.Services.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Hoscy.Services
{
    public static class Media
    {
        private static GlobalSystemMediaTransportControlsSessionManager? _gsmtcsm;
        private static GlobalSystemMediaTransportControlsSession? _session;
        private static GlobalSystemMediaTransportControlsSessionMediaProperties? _nowPlaying;

        #region Startup
        public static void StartMediaDetection()
            => Task.Run(() => StartMediaDetectionInternal()).ConfigureAwait(false);

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

            if (playbackInfo == null || playbackInfo.PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing || newPlaying.Title == null)
            {
                ClearNowPlaying();
                return;
            }

            lock (_lock) //We use this lock here as media can sometimes end up here twice at the same time
            {
                if (_nowPlaying != null)
                    if (newPlaying.Artist == _nowPlaying.Artist && newPlaying.Title == (_nowPlaying.Title ?? ""))
                        return;
            }

            _nowPlaying = newPlaying;

            if (Config.Textbox.ShowMediaStatus)
            {
                if (string.IsNullOrWhiteSpace(_nowPlaying.Title))
                {
                    ClearNowPlaying();
                    return;
                }

                var playing = $"'{_nowPlaying.Title}'";
                if (!string.IsNullOrWhiteSpace(_nowPlaying.Artist))
                    playing += $" by '{_nowPlaying.Artist}'";

                Logger.Log($"Currently playing media has changed to: {playing}");
                Textbox.Notify($"Listening to {playing}", NotificationType.Media);
            }
        }
        private static void UpdateCurrentlyPlayingMediaProxy(GlobalSystemMediaTransportControlsSession sender)
            => Task.Run(() => UpdateCurrentlyPlayingMedia(sender)).ConfigureAwait(false);

        private static void ClearNowPlaying()
            => Textbox.Notify(string.Empty, NotificationType.Media);
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
            Test
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
            { "back", MediaCommandType.Rewind }
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
            Task.Run(async () => await HandleMediaCommand(mediaCommand)).ConfigureAwait(false);
        }

        /// <summary>
        /// Actual handling of media commands
        /// </summary>
        /// <param name="command">command type</param>
        private async static Task HandleMediaCommand(MediaCommandType command)
        {
            if (_session == null)
                return;

            switch(command)
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

                default: return;
            }
        }
        #endregion
    }
}
