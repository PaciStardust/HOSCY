using System;

namespace Hoscy.Services.Api
{
    internal static class SoundPlayer
    {
        private static readonly System.Media.SoundPlayer _player = new();

        internal static void Play(Sound sound)
        {
            var resName = $"Hoscy.Resources.{sound}.wav";
            try
            {
                Logger.Debug($"Attempting to load and play sound  \"{resName}\"");
                _player.Stream = Utils.GetEmbeddedRessourceStream(resName);
                _player.Load();
                _player.Play();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unable to load and play sound \"{resName}\"");
            }
        }

        internal enum Sound
        {
            Mute,
            Unmute
        }
    }
}
