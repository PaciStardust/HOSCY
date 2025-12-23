using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Osc.Misc;

/// <summary>
/// Handling of OSC commands
/// </summary>
public interface IOscCommandService : IAutoStartStopService
{
    /// <summary>
    /// Checks if a string of text is an OSC command
    /// </summary>
    /// <returns>Is Command?</returns>
    public bool DetectCommand(string commandString);

    /// <summary>
    /// Executes OscCommandString
    /// </summary>
    /// <returns>Success</returns>
    public OscCommandState HandleCommand(string commandString);

    /// <summary>
    /// Checks if a string of text is an OSC command and executes it
    /// </summary>
    /// <returns>Success</returns>
    public OscCommandState DetectAndHandleCommand(string commandString);

    /// <summary>
    /// Gets the text needed to identify a command
    /// </summary>
    public string GetCommandIdentifier();
}

public enum OscCommandState
{
    Success,
    NotCommand,
    Malformed,
    Shutdown
}