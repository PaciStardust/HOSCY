namespace HoscyCli.Commands;

public class CommandCollection
{
    private readonly List<CommandCollectionElement> _commands = [];

    public CommandCollection Register(CommandCollectionElement element)
    {
        _commands.Add(element);
        return this;
    }

    public CommandCollection Register(string[] keywords, Func<CommandResult> func)
    {
        return Register(new(keywords, func));
    }
}


public readonly struct CommandCollectionElement(string[] keywords, Func<CommandResult> func)
{
    public string[] Keywords { get; } = keywords;
    public Func<CommandResult> Func { get; } = func;
}