using System.Reflection;

namespace HoscyCli.Commands.Core;

public abstract class AttributeCommandModule : ICommandModule
{
    private readonly List<(SubCommandModuleAttribute Attribute, Func<string?, CommandResult> Func)> _commandInfo = [];

    public AttributeCommandModule()
    {
        var methods = GetType().GetMethods();
        foreach(var method in methods)
        {
            var attribute = method.GetCustomAttribute<SubCommandModuleAttribute>();
            if (attribute is null) continue;

            if (method.ReturnType != typeof(CommandResult))
            {
                throw new ArgumentException($"Method {method.Name} does not return type {nameof(CommandResult)}");
            }

            var methodParameters = method.GetParameters();
            if (methodParameters.Length > 1)
            {
                throw new ArgumentException($"Method {method.Name} has more than 1 parameter");
            }

            if (methodParameters.Length == 0)
            {
                _commandInfo.Add((attribute, _ => (CommandResult)method.Invoke(this, [])!));
                continue;
            }

            if (methodParameters[0].ParameterType != typeof(string))
            {
                throw new ArgumentException($"Method {method.Name} parameter is not string");
            }

            _commandInfo.Add((attribute, x => (CommandResult)method.Invoke(this, [x])!));
        }
    }

    public CommandResult ExecutePrependArgs(string command, string prependArgs)
    {
        var (Command, Parameters) = Util.SplitAtFirstSpace(command);
        return string.IsNullOrWhiteSpace(Parameters)
            ? Execute(Command, prependArgs)
            : Execute(Command, string.Join(" ", prependArgs, Parameters));
    }

    public CommandResult Execute(string command)
    {
        var (Command, Parameters) = Util.SplitAtFirstSpace(command);
        return Execute(Command, Parameters);
    }

    public CommandResult Execute(string command, string? args)
    {
        var match = GetSubCommandIndex(command);
        if (match != -1) return _commandInfo[match].Func(args);
        Console.WriteLine($"{GetType().Name} Unable to locate command {command}");
        return CommandResult.NotFound;
    }

    private int GetSubCommandIndex(string command)
    {
        var match = _commandInfo.FindIndex(x => x.Attribute.ShouldHandle(command, CommandIdentifierMatchMode.Full));
        if (match != -1) return match;
            match = _commandInfo.FindIndex(x => x.Attribute.ShouldHandle(command, CommandIdentifierMatchMode.Start));
        if (match != -1) return match;
        return _commandInfo.FindIndex(x => x.Attribute.ShouldHandle(command, CommandIdentifierMatchMode.Contains));
    }

    [SubCommandModule(["help", "list", "l", "h", "?"], "Lists all available commands.")]
    public CommandResult List()
    {
        var print = string.Join("\n", _commandInfo.Select(info => $" - {string.Join("/", info.Attribute.Identifiers[0])} - {info.Attribute.Description}"));
        Console.WriteLine($"Available Commands:\n{print}");
        return CommandResult.Success;
    }
}