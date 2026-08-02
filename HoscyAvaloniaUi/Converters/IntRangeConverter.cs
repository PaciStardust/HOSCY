using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace HoscyAvaloniaUi.Converters;

public class IntRangeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string strParam)
        {
            return new BindingNotification(new Exception("Failed to get parameters"), BindingErrorType.Error);
        }
        if (value is not string strValue)
        {
            return new BindingNotification(new Exception("Failed to get input string"), BindingErrorType.Error);
        }
        var split = strParam.Split('-');
        if (split.Length != 2 || !int.TryParse(split[0], out var min) || !int.TryParse(split[1], out var max))
        {
            return new BindingNotification(new Exception("Failed to get parameters correctly"), BindingErrorType.Error);
        }
        if (!int.TryParse(strValue, out var intValue))
        {
            return new BindingNotification(new Exception("Provided value is not an integer"), BindingErrorType.Error);
        }
        if (intValue < min || intValue > max)
        {
            return new BindingNotification(new Exception($"Provided value should be between {min} and {max}"), BindingErrorType.Error);
        }
        return intValue;
    }
}