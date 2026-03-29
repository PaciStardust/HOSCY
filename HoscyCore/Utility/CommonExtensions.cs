namespace HoscyCore.Utility;

public static class CommonExtensions
{
    /// <summary>
    /// Runs an async Task without awaiting
    /// </summary>
    /// <param name="task">Task to be run</param>
    public static void RunWithoutAwait(this Task task)
        => Task.Run(async () => await task).ConfigureAwait(false);

    /// <summary>
    /// Makes the first character of a string into an uppercase char
    /// </summary>
    /// <param name="input">String to modify</param>
    /// <returns>Modified string</returns>
    public static string FirstCharToUpper(this string input) =>
        string.IsNullOrEmpty(input) ? input : string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));

    /// <summary>
    /// Returns index of first element matching predicate
    /// </summary>
    public static int GetListIndex<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// A combination of floor and ceil for comparables
    /// </summary>
    /// <typeparam name="T">Type to compare</typeparam>
    /// <param name="value">Value to compare</param>
    /// <param name="min">Minimum value</param>
    /// <param name="max">Maximum value</param>
    /// <returns>Value, if within bounds. Min, if value smaller than min. Max, if value larger than max. If max is smaller than min, min has priority</returns>
    public static T MinMax<T>(this T value, T min, T max) where T : IComparable
    {
        if (value.CompareTo(min) < 0)
            return min;
        if (value.CompareTo(max) > 0)
            return max;
        return value;
    }

    /// <summary>
    /// Converts an int to a ushort
    /// </summary>
    /// <param name="value">int to convert</param>
    /// <returns>ushort.MinValue when value is out of bounds, otherwise value as ushort</returns>
    public static ushort ConvertToUshort(this int value)
    {
        if (value > ushort.MaxValue || value < ushort.MinValue) return ushort.MinValue;
        return (ushort)(value & ushort.MaxValue); //Making sure we are truly within bit bounds
    }
}