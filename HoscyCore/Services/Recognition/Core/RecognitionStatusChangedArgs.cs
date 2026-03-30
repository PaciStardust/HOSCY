using HoscyCore.Services.Core;

namespace HoscyCore.Services.Recognition.Core;

public class RecognitionStatusChangedEventArgs(bool listening, ServiceStatus status) : EventArgs
{
    public bool IsListening { get; init; } = listening;
    public ServiceStatus Status { get; init; } = status;
}