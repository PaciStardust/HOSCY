namespace HoscyCli.Commands;

public class CommandCollection
{
    private readonly List<CommandCollectionElement> _commands = [];

    public CommandCollection Register(CommandCollectionElement element)
    {
        _commands.Add(element);
        return this;
    }

    public CommandCollection Register(string[] keywords, Func<string?, CommandResult> func)
    {
        return Register(new(keywords, func));
    }

    public CommandResult Execute(string command, string? parameters)
    {
        var match = GetCommandIndex(command);
        return match == -1
            ? CommandResult.NotFound
            : _commands[match].Execute(parameters);
    }

    public CommandResult Execute((string Command, string? Parameters) commmandTuple)
    {
        return Execute(commmandTuple.Command, commmandTuple.Parameters);
    }

    public CommandResult Execute(string command)
    {
        return Execute(CommandUtils.SplitAtFirstSpace(command));
    }

    private int GetCommandIndex(string command)
    {
        var match = _commands.FindIndex(x => x.Matches(command, CommandMatchMode.Full));
        if (match != -1) return match;
            match = _commands.FindIndex(x => x.Matches(command, CommandMatchMode.Start));
        if (match != -1) return match;
        return _commands.FindIndex(x => x.Matches(command, CommandMatchMode.Contains));
    }
}


public readonly struct CommandCollectionElement(string[] keywords, Func<string?, CommandResult> func)
{
    public string[] Keywords { get; } = keywords;
    public Func<string?, CommandResult> Func { get; } = func;

    public bool Matches(string command, CommandMatchMode matchMode)
    {
        return matchMode switch
        {
            CommandMatchMode.Full => Keywords.Any(x => x.Equals(command, StringComparison.OrdinalIgnoreCase)),
            CommandMatchMode.Start => Keywords[0].StartsWith(command, StringComparison.OrdinalIgnoreCase),
            CommandMatchMode.Contains => Keywords[0].Contains(command, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    public CommandResult Execute(string? parameters)
    {
        return Func.Invoke(parameters);
    }
}