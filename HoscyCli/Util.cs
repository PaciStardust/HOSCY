namespace HoscyCli;

public static class Util
{
    public static (string Command, string? Parameters) SplitAtFirstSpace(string input)
    {
        var spaceIndex = input.IndexOf(' ');
        if (spaceIndex == -1)
            return new(input, null);
        if (spaceIndex == input.Length - 1)
            return new(input[..^1], null);
        return new(input[..spaceIndex], input[(spaceIndex + 1)..]);
    }

    public static void DisplayEx(Exception e)
    {
        Console.WriteLine($"{e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
    }
}