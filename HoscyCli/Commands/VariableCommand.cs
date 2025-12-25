namespace HoscyCli.Commands;

public abstract class VariableCommand()
{
    public void Execute(string command)
    {
        var commandParts = CommandUtils.SplitAtFirstSpace(command);
        //todo: handling
    }

    protected abstract void SetVariable(string command);
    protected abstract (string VariableName, string Value) GetVariableInfo();
}