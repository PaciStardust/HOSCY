using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Hoscy.Services.Speech.Utilities
{
    /// <summary>
    /// Class to use for windows recognizer as Stream, 
    /// 
    /// Credits to "Sean" on Stackoverflow
    /// https://stackoverflow.com/questions/1682902/streaming-input-to-system-speech-recognition-speechrecognitionengine
    /// </summary>

    public class SpeechStreamer : Stream
    {
        private AutoResetEvent? _writeEvent;
        private readonly List<byte> _buffer;
        private readonly int _buffersize;
        private int _readposition;
        private int _writeposition;
        private bool _reset;

        public SpeechStreamer(int bufferSize)
        {
            _writeEvent = new AutoResetEvent(false);
            _buffersize = bufferSize;
            _buffer = new List<byte>(_buffersize);
            for (int i = 0; i < _buffersize; i++)
                _buffer.Add(new byte());
            _readposition = 0;
            _writeposition = 0;
        }

        public override bool CanRead
            => true;

        public override bool CanSeek
            => false;

        public override bool CanWrite
            => true;

        public override long Length
            => -1L;

        public override long Position
        {
            get => 0L;
            set { }
        }

        public override long Seek(long offset, SeekOrigin origin)
            => 0L;

        public override void SetLength(long value) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int i = 0;
            while (i < count && _writeEvent != null)
            {
                if (!_reset && _readposition >= _writeposition)
                {
                    _writeEvent.WaitOne(100, true);
                    continue;
                }
                buffer[i] = _buffer[_readposition + offset];
                _readposition++;
                if (_readposition == _buffersize)
                {
                    _readposition = 0;
                    _reset = false;
                }
                i++;
            }

            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_writeEvent == null)
                return;

            for (int i = offset; i < offset + count; i++)
            {
                _buffer[_writeposition] = buffer[i];
                _writeposition++;
                if (_writeposition == _buffersize)
                {
                    _writeposition = 0;
                    _reset = true;
                }
            }
            _writeEvent.Set();
        }

        public override void Close()
        {
            _writeEvent?.Close();
            _writeEvent = null;
            base.Close();
        }

        public override void Flush() { }
    }
}
