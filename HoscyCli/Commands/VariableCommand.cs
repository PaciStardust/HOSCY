namespace HoscyCli.Commands;

//todo: this is bad, this should be a singleton, same with converters
public class VariableCommand<T>
{
    private readonly CommandCollection _commands;
    private readonly Func<(string Name, string Value)> _getInfo;
    private readonly Action<T> _setValue;
    private readonly Func<string, T> _converter;


    public VariableCommand(Func<(string Name, string Value)> getInfo, Action<T> setValue, Func<string, T> converter)
    {
        _commands = GenerateCommands();
        _getInfo = getInfo;
        _setValue = setValue;
        _converter = converter;
    }

    public CommandResult Execute(string command)
    {
        return _commands.Execute(command);
    }

    private CommandCollection GenerateCommands()
    {
        return new CommandCollection()
            .Register(["set", "s"], SetVariable)
            .Register(["get", "g"], _ => GetVariable());
    }

    private CommandResult GetVariable()
    {
        var (variableName, value) = _getInfo();
        Console.WriteLine($"Parameter {variableName} is set to: {variableName}");
        return CommandResult.Success;
    } 
    protected CommandResult SetVariable(string? command)
    {
        if (command is null) return CommandResult.MissingParameter;
        var converted  = _converter(command);
        _setValue(converted);
        GetVariable();
        return CommandResult.Success;
    }
}