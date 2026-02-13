using HoscyCore.Services.Osc.Command;

namespace HoscyCoreTests.Mocks;

public class MockOscCommandService : MockStartStopServiceBase, IOscCommandService
{
    public required string CommandIdentifier { get; init; }

    public List<string> ReceivedStrings { get; init; } = []; 
    public List<string> PassedStrings { get; init; } = []; 
    public OscCommandState ReturnedState { get; set; } = OscCommandState.Success;

    public OscCommandState DetectAndHandleCommand(string commandString)
    {
        if (!DetectCommand(commandString)) return OscCommandState.NotCommand;
        return HandleCommand(commandString);
    }

    public bool DetectCommand(string commandString)
    {
        ReceivedStrings.Add(commandString);
        return commandString.StartsWith(CommandIdentifier);
    }

    public OscCommandState HandleCommand(string commandString)
    {
        PassedStrings.Add(commandString);
        return ReturnedState;
    }
}