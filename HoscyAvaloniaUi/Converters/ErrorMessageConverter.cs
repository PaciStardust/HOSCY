using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace HoscyAvaloniaUi.Converters;

public class ErrorMessageConverter : IValueConverter
{
    private static readonly Regex _castExceptionExtractor = new(@"Could not convert '{(?:[^:]+: )+([^}]+)?}'");
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is BindingNotification notification)
        {
            if (notification.Error is Exception exSub)
                return exSub.Message;
            return notification.Error?.ToString() ?? "Validation error";
        }
        if (value is string s) return s;
        if (value is InvalidCastException icex)
        {
            var m = _castExceptionExtractor.Match(icex.Message);
            if (m is not null && m.Success) return m.Groups[1];
        }
        if (value is Exception ex) return ex.Message;
        if (value is ValidationResult vr) return vr.ErrorMessage ?? string.Empty;
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}