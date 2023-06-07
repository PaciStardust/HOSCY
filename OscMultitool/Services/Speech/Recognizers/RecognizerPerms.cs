namespace Hoscy.Services.Speech.Recognizers
{
    internal readonly struct RecognizerPerms
    {
        public RecognizerPerms() { }
        internal readonly RecognizerType Type { get; init; } = RecognizerType.None;
        internal readonly bool UsesMicrophone { get; init; } = false;
        internal readonly string Description { get; init; } = "No info available";
    }

    internal enum RecognizerType
    {
        None,
        Vosk,
        Whisper,
        Windows,
        Azure,
        AnyApi
    }
}
