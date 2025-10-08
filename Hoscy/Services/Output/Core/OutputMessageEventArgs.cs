using System;

namespace Hoscy.Services.Output.Core;

public class OutputMessageEventArgs(string contents) : EventArgs
{
    public string Contents { get; init; } = contents;
}