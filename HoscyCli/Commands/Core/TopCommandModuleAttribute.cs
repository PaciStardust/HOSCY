namespace HoscyCli.Commands.Core;

[AttributeUsage(AttributeTargets.Class)]
public class TopCommandModuleAttribute(string[] identifiers) : CommandAttributeBase(identifiers) {}