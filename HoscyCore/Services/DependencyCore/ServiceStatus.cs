namespace HoscyCore.Services.DependencyCore;

public enum ServiceStatus
{
    Stopped = 0b0,
    Started = 0b1,
    Faulted = 0b11,
    Processing = 0b101,
}