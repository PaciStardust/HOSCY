namespace Hoscy.Services.Speech.Recognizers
{
    internal readonly struct RecognizerPerms
    {
        public RecognizerPerms() { }
        internal readonly bool UsesVoskModel { get; init; } = false;
        internal readonly bool UsesWinRecognizer { get; init; } = false;
        internal readonly bool UsesMicrophone { get; init; } = false;
        internal readonly string Description { get; init; } = "No info available";
    }
}
