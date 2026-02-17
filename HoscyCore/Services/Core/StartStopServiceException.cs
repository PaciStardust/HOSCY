namespace HoscyCore.Services.Core;

/// <summary>
/// Represents an exception when starting or stopping a service
/// </summary>
public class StartStopServiceException(string message) : Exception(message)
{
}