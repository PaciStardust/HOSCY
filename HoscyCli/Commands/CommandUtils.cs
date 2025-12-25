namespace HoscyCli.Commands;

public static class CommandUtils
{
    public static SplitCommand SplitAtFirstSpace(string input)
    {
        return SplitAtFirstSpace(input.AsSpan());
    }

    public static SplitCommand SplitAtFirstSpace(ReadOnlySpan<char> input)
    {
        var spaceIndex = input.IndexOf(' ');
        if (spaceIndex == -1)
            return new(input, null);
        if (spaceIndex == input.Length - 1)
            return new(input[..^1], null);
        return new(input[..spaceIndex], input[(spaceIndex + 1)..]);
    }
}

public readonly ref struct SplitCommand(ReadOnlySpan<char> command, ReadOnlySpan<char> parameters)
{
    public ReadOnlySpan<char> Command { get; } = command;
    public ReadOnlySpan<char> Parameters { get; } = parameters;
}