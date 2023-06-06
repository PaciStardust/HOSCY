using Hoscy.Services.Speech.Utilities;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Vosk;

namespace Hoscy.Services.Speech.Recognizers
{
    internal class RecognizerVosk : RecognizerBase
    {
        new internal static RecognizerPerms Perms => new()
        {
            Description = "Local AI, quality / RAM usage varies, startup may take a while",
            UsesMicrophone = true,
            Type = RecognizerType.Vosk
        };

        internal override bool IsListening => _microphone.IsListening;

        private readonly WaveInProxy _microphone = new();
        private VoskRecognizer? _rec;

        //Used for stopping thread
        private Thread? _recThread;
        private bool _threadStop = false;
            
        #region Start / Stop and Muting
        protected override bool StartInternal()
        {
            try
            {
                Logger.Info("Attempting to load vosk model, this might take a while");
                var valid = Config.Speech.VoskModels.TryGetValue(Config.Speech.VoskModelCurrent, out var path);
                if (!valid || !Directory.Exists(path))
                {
                    Logger.Error("A Vosk AI model has not been picked or it's path is invalid.\n\nTo use Vosk speech recognition please provide an AI model. Information can be found in the quickstart guide on GitHub\n\nIf you do not want to use Vosk, please change the recognizer type on the speech page");
                    return false;
                }
                var model = new Model(path);

#nullable disable
                //Using reflection to get handle (Checking if fails to initalize model)
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                FieldInfo handleField = typeof(Model).GetField("handle", bindFlags);
                var modelhandleInternal = (System.Runtime.InteropServices.HandleRef)handleField.GetValue(model);
                if (modelhandleInternal.Handle == IntPtr.Zero)
                {
                    Logger.Error("Attempted to start model but the picked recognition model file is invalid. Have you downloaded a compatible model, picked the correct folder and verified it is not corrupt?");
                    return false;
                }
#nullable enable

                _rec = new(model, 16000);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to start Vosk speech recognition.");
                return false;
            }

            _microphone.DataAvailable += OnDataAvailable;
            _microphone.Start();
            _recThread = new(new ThreadStart(HandleAvailableDataLoop))
            {
                Name = "Vosk Data Handler",
                Priority = ThreadPriority.AboveNormal
            };
            _recThread.Start();
            return true;
        }

        protected override void StopInternal()
        {
            HandleSpeechChanged(false);
            _microphone.Dispose();

            //Stopping thread
            _threadStop = true;
            _recThread?.Join();
            _recThread = null;

            //Clearing the rest
            _rec?.Dispose();
            _rec = null;
        }

        protected override bool SetListeningInternal(bool enabled)
            => _microphone.SetMuteStatus(enabled);
        #endregion

        #region Result handling
        private DateTime _lastChangedAt = DateTime.MaxValue;
        private string _lastChangedString = string.Empty;

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
            => _dataAvailableQueue.Enqueue(e.Buffer);

        private readonly Queue<byte[]> _dataAvailableQueue = new();
        /// <summary>
        /// This loop handles all recognition on a seperate thread as it causes lag otherwise, it checks a queue to avoid data getting mixed up
        /// This is the only way I found to nicely do this off-main thread as the microphone really loves to cause AccessViolationExceptions
        /// This took me way too long to figure out but taught me a lot about threading so I cant complain
        /// </summary>
        private void HandleAvailableDataLoop()
        {
            Logger.PInfo("Started Vosk recognizer data handling thread");
            while (_rec != null && !_threadStop)
            {
                if (_dataAvailableQueue.Count == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }

                var data = _dataAvailableQueue.Dequeue();

                if (data == null)
                    continue;

                if (_rec.AcceptWaveform(data, data.Length))
                    HandleResultComplete();
                else
                    HandleResultIncomplete();
            }
            Logger.PInfo("Stopped Vosk recognizer data handling thread");
        }

        /// <summary>
        /// Handles a complete recognizer result
        /// </summary>
        private void HandleResultComplete()
        {
            if (_rec == null)
            {
                Logger.Warning("Attempted to handle complete result on disabled recognizer");
                return;
            }

            var result = CleanMessage(_rec.Result());
            if (!string.IsNullOrWhiteSpace(result))
            {
                Logger.Log("Got Message: " + result);
                HandleSpeechRecognized(result);
            }

            ClearLastChanged();
        }

        /// <summary>
        /// Handling of incomplete recognizer results, a T indicates that it was forced with timeout
        /// </summary>
        private void HandleResultIncomplete()
        {
            if (_rec == null)
            {
                Logger.Warning("Attempted to handle incomplete result on disabled recognizer");
                return;
            }

            var result = CleanMessage(_rec.PartialResult());
            if (string.IsNullOrWhiteSpace(result))
                return;

            if (_lastChangedString != result)
            {
                _lastChangedString = result;
                _lastChangedAt = DateTime.Now;
                HandleSpeechChanged(true);
                return;
            }

            if ((DateTime.Now - _lastChangedAt).TotalMilliseconds > Config.Speech.VoskTimeout)
            {
                Logger.Log("Got Message (T): " + result);
                ClearLastChanged();
                HandleSpeechRecognized(result);
            }
        }

        /// <summary>
        /// Utility for clearing all recognizer related info
        /// </summary>
        private void ClearLastChanged()
        {
            HandleSpeechChanged(false);
            _lastChangedString = string.Empty;
            _lastChangedAt = DateTime.MaxValue;
            _rec?.Reset();
        }
        #endregion

        #region Cleanup
        private static string? CleanMessage(string res)
        {
            var extracted = Utils.ExtractFromJson(string.Empty, res);
            //todo: [TEST] Does this avoid noise causing a typing indicator?
            if (extracted == null || Config.Speech.NoiseFilter.Contains(extracted))
                return null;

            return extracted;
        }
        #endregion
    }
}
