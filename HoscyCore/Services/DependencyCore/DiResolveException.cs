namespace HoscyCore.Services.DependencyCore;

/// <summary>
/// Represents an exception when resolving dependencies
/// </summary>
public class DiResolveException(string message) : Exception(message)
{
}