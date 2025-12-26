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

    public CommandResult Execute(string command)
    {
        var (Command, Parameters) = SplitAtFirstSpace(command);
        return Execute(Command, Parameters);
    }

    public CommandResult Execute(string command, string? args)
    {
        var match = GetSubCommandIndex(command);
        return match == -1
            ? CommandResult.NotFound
            : _commandInfo[match].Func(args);
    }

    private int GetSubCommandIndex(string command)
    {
        var match = _commandInfo.FindIndex(x => x.Attribute.ShouldHandle(command, CommandIdentifierMatchMode.Full));
        if (match != -1) return match;
            match = _commandInfo.FindIndex(x => x.Attribute.ShouldHandle(command, CommandIdentifierMatchMode.Start));
        if (match != -1) return match;
        return _commandInfo.FindIndex(x => x.Attribute.ShouldHandle(command, CommandIdentifierMatchMode.Contains));
    }

    private static (string Command, string? Parameters) SplitAtFirstSpace(string input)
    {
        var spaceIndex = input.IndexOf(' ');
        if (spaceIndex == -1)
            return new(input, null);
        if (spaceIndex == input.Length - 1)
            return new(input[..^1], null);
        return new(input[..spaceIndex], input[(spaceIndex + 1)..]);
    }

    [SubCommandModule(["list", "help", "l", "h"], "Lists all available commands.")]
    public CommandResult List()
    {
        var print = string.Join("\n", _commandInfo.Select(info => $"{string.Join("/", info.Attribute.Identifiers[0])} - {info.Attribute.Description}"));
        Console.WriteLine($"Available Commands:\n{print}");
        return CommandResult.Success;
    }
}