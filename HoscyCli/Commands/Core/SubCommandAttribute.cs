namespace HoscyCli.Commands.Core;

[AttributeUsage(AttributeTargets.Class)]
public class SubCommandModuleAttribute(string[] identifiers) : CommandAttributeBase(identifiers) {}