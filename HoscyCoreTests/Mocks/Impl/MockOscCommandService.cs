using HoscyCore.Services.Osc.Command;
using HoscyCore.Utility;
using HoscyCoreTests.Mocks.Base;

namespace HoscyCoreTests.Mocks.Impl;

public class MockOscCommandService : MockStartStopServiceBase, IOscCommandService
{
    public required string CommandIdentifier { get; init; }

    public List<string> ReceivedStrings { get; init; } = []; 
    public List<string> PassedStrings { get; init; } = []; 
    public Res<OscCommandState> ReturnedState { get; set; } = ResC.TOk(OscCommandState.Success);

    public Res<OscCommandState> DetectAndHandleCommand(string commandString)
    {
        if (!DetectCommand(commandString)) return ResC.TOk(OscCommandState.NotCommand);
        return HandleCommand(commandString);
    }

    public bool DetectCommand(string commandString)
    {
        ReceivedStrings.Add(commandString);
        return commandString.StartsWith(CommandIdentifier);
    }

    public Res<OscCommandState> HandleCommand(string commandString)
    {
        PassedStrings.Add(commandString);
        return ReturnedState;
    }
}