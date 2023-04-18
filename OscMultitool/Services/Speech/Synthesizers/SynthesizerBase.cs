using System.IO;
using System.Threading.Tasks;

namespace Hoscy.Services.Speech.Synthesizers
{
    internal abstract class SynthesizerBase
    {
        protected MemoryStream? _stream;

        internal SynthesizerBase(MemoryStream stream)
        {
            _stream = stream;
        }

        internal abstract bool IsAsync { get; }

        internal abstract bool Speak(string text);
        internal abstract Task<bool> SpeakAsync(string text);
    }
}
