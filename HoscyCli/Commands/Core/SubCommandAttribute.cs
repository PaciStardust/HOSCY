namespace HoscyCli.Commands.Core;

[AttributeUsage(AttributeTargets.Method)]
public class SubCommandModuleAttribute(string[] identifiers, string description = "") : Attribute
{
    public string[] Identifiers { get; } = identifiers;
    public string Description { get; } = description;

    public bool ShouldHandle(string command, CommandIdentifierMatchMode matchMode)
    {
        return matchMode switch
        {
            CommandIdentifierMatchMode.Full => Identifiers.Any(x => x.Equals(command, StringComparison.OrdinalIgnoreCase)),
            CommandIdentifierMatchMode.Start => Identifiers[0].StartsWith(command, StringComparison.OrdinalIgnoreCase),
            CommandIdentifierMatchMode.Contains => Identifiers[0].Contains(command, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}