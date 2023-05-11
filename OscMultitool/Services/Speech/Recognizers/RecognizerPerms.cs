namespace Hoscy.Services.Speech.Recognizers
{
    internal readonly struct RecognizerPerms //todo: [REFACTOR] renames, get func
    {
        public RecognizerPerms() { }
        internal readonly bool UsesVoskModel { get; init; } = false;
        internal readonly bool UsesWhisperModel { get; init; } = false;
        internal readonly bool UsesWinRecognizer { get; init; } = false;
        internal readonly bool UsesAzureApi { get; init; } = false;
        internal readonly bool UsesAnyApi { get; init; } = false;
        internal readonly bool UsesMicrophone { get; init; } = false;
        internal readonly string Description { get; init; } = "No info available";
    }
}
