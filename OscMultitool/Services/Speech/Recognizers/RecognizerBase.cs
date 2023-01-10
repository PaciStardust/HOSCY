using Hoscy.Services.OscControl;
using Hoscy.Services.Speech.Recognizers;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hoscy.Services.Speech
{
    public abstract class RecognizerBase
    {
        public static RecognizerPerms Perms => new();
        public bool IsRunning { get; private set; } = false;
        public abstract bool IsListening { get; }

        /// <summary>
        /// Init
        /// </summary>
        public RecognizerBase() { }

        #region Starting / Stopping
        /// <summary>
        /// Override for internal start procedures
        /// </summary>
        /// <returns>Success</returns>
        abstract protected bool StartInternal();

        /// <summary>
        /// Start the listener
        /// </summary>
        /// <returns>Success</returns>
        public bool Start()
        {
            if (IsRunning)
            {
                Logger.Warning("Tried to start recognizer while it is already running");
                return true;
            }

            IsRunning = StartInternal();

            if (!IsRunning)
                return false;

            //Reloading Denoiser
            UpdateDenoiseRegex();

            if (Config.Speech.StartUnmuted)
                SetListening(true);

            return true;
        }

        /// <summary>
        /// Override for internal stop procedures, please dispose of anything, restarting is not intended
        /// </summary>
        abstract protected void StopInternal();

        /// <summary>
        /// Stops the listener
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                Logger.Warning("Tried to stop recognizer while it is not running");
                return;
            }

            SetListening(false);
            StopInternal();
            IsRunning = false;
        }
        #endregion

        #region Mic
        /// <summary>
        /// Enables or disables listening
        /// </summary>
        /// <param name="enabled">Mode</param>
        public bool SetListening(bool enabled)
        {
            if (!IsRunning || IsListening == enabled)
                return false;

            bool result = SetListeningInternal(enabled);
            if (!result)
                Logger.Warning("Failed to change mic status");
            else
            {
                var packet = new OscPacket(Config.Osc.AddressListeningIndicator, enabled);//Ingame listening indicator
                if (!packet.IsValid)
                    Logger.Warning("Unable to send data to ingame listening indicator, packet is invalid");
                else
                    Osc.Send(packet);
            }

            return result;
        }

        /// <summary>
        /// Enables or disables listening, internal procedures
        /// </summary>
        /// <param name="enabled">Mode</param>
        protected abstract bool SetListeningInternal(bool enabled);
        #endregion

        #region Processing
        /// <summary>
        /// Processing and sending off the message
        /// </summary>
        public static void ProcessMessage(string message)
        {
            if (Config.Speech.RemoveFullStop)
                message = message.TrimEnd('.');

            var processor = new TextProcessor()
            {
                TriggerReplace = Config.Speech.UseReplacements,
                ReplaceCaseInsensitive = Config.Speech.IgnoreCaps,

                TriggerCommands = true,

                UseTextbox = Config.Speech.UseTextbox,
                UseTts = Config.Speech.UseTts,

                AllowTranslate = true
            };

            processor.Process(message);
        }
        #endregion

        #region Denoising
        private static Regex _denoiseFilter = new(" *");

        /// <summary>
        /// Removes "noise" from message
        /// </summary>
        protected static string Denoise(string message)
        {
            message = message.Trim();

            if (!_denoiseFilter.IsMatch(message))
                return string.Empty;

            message = _denoiseFilter.Match(message).Groups[1].Value.Trim();

            return message;
        }

        /// <summary>
        /// Generates a regex for denoising
        /// </summary>
        public static void UpdateDenoiseRegex()
        {
            RegexOptions opt = Config.Speech.IgnoreCaps ? RegexOptions.IgnoreCase : RegexOptions.None;

            var filterWords = Config.Speech.NoiseFilter.Select(x => $"(?:{x})");
            var filterCombined = string.Join('|', filterWords);
            var regString = $"^(?:\\b(?:{filterCombined})\\b)?(.*?)(?:\\b(?:{filterCombined})\\b)?$";
            Logger.PInfo($"Updated denoiser ({regString})");
            _denoiseFilter = new Regex(regString, opt | RegexOptions.CultureInvariant);
        }
        #endregion
    }
}
