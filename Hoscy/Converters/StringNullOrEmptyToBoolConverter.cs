using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Hoscy.Converters;

public class StringNullOrEmptyToBoolConverter : IValueConverter
{
    public static readonly StringNullOrEmptyToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        return string.IsNullOrEmpty(s);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}