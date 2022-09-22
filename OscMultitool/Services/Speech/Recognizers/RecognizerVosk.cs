using Hoscy;
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
    public class RecognizerVosk : RecognizerBase
    {
        new public static RecognizerPerms Perms => new()
        {
            UsesMicrophone = true,
            UsesVoskModel = true
        };

        public override bool IsListening => _microphone.IsListening;

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
                if (!Directory.Exists(Config.Speech.VoskModelPath))
                {
                    Logger.Error("The provided filepath for the model does not exist, did you download a model?");
                    return false;
                }

                var model = new Model(Config.Speech.VoskModelPath);

#nullable disable
                //Using reflection to get handle (Checking if fails to initalize model)
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                FieldInfo handleField = typeof(Model).GetField("handle", bindFlags);
                var modelhandleInternal = (System.Runtime.InteropServices.HandleRef)handleField.GetValue(model);
                if (modelhandleInternal.Handle == IntPtr.Zero)
                {
                    Logger.Error("Attempted to start model but the picked recognition model file is invalid. Have you downloaded a compatible model and verified it is not corrupt?");
                    return false;
                }
#nullable enable

                _rec = new(model, 16000);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return false;
            }

            _microphone.DataAvailable += OnDataAvailable;
            _microphone.Start();
            _recThread = new(new ThreadStart(HandleAvailableDataLoop));
            _recThread.Start();
            return true;
        }

        protected override void StopInternal()
        {
            Textbox.EnableTyping(false);
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

            string result = CleanMessage(_rec.Result());

            if (!string.IsNullOrWhiteSpace(result))
            {
                Logger.Log("Got Message: " + result);
                ProcessMessage(result);
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

            string result = CleanMessage(_rec.PartialResult());

            if (string.IsNullOrWhiteSpace(result))
                return;

            if (_lastChangedString != result)
            {
                _lastChangedString = result;
                _lastChangedAt = DateTime.Now;
                Textbox.EnableTyping(true);
                return;
            }

            if ((DateTime.Now - _lastChangedAt).TotalMilliseconds > Config.Speech.VoskTimeout)
            {
                Logger.Log("Got Message (T): " + result);
                ProcessMessage(result);
                ClearLastChanged();
            }
        }

        /// <summary>
        /// Utility for clearing all recognizer related info
        /// </summary>
        private void ClearLastChanged()
        {
            Textbox.EnableTyping(false);
            _lastChangedString = string.Empty;
            _lastChangedAt = DateTime.MaxValue;
            _rec?.Reset();
        }
        #endregion

        #region Cleanup
        private static string CleanMessage(string res)
            => Denoise(TextProcessor.ExtractFromJson(string.Empty, res));
        #endregion
    }
}
