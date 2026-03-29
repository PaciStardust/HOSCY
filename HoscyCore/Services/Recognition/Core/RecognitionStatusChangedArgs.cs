namespace HoscyCore.Services.Recognition.Core;

public class RecognitionStatusChangedEventArgs(bool listening, bool started) : EventArgs
{
    public bool IsListening { get; init; } = listening;
    public bool IsStarted { get; init; } = started;
}