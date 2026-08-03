using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Converters;
using HoscyCore.Utility;

namespace HoscyAvaloniaUi.Converters;

public abstract class BaseRangeConverter<T> : IValueConverter where T: IComparable
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }

    protected abstract Func<string, (bool, T)> Converter { get; }
    protected abstract T Min { get; }
    protected abstract T Max { get; }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var res = RangeConverterUtils.PerformFull(value, parameter, Converter, Min, Max);
        if (res is null) return null;
        return res.IsOk ? res.Value : new BindingNotification(new Exception(res.Msg.Message), BindingErrorType.DataValidationError); 
    }
}

public class IntRangeConverter : BaseRangeConverter<int>
{
    protected override Func<string, (bool, int)> Converter => (x) => int.TryParse(x, out var y) ? (true, y) : (false, Min);
    protected override int Min => int.MinValue;
    protected override int Max => int.MaxValue;
}

public class FloatRangeConverter : BaseRangeConverter<float>
{
    protected override Func<string, (bool, float)> Converter => (x) => float.TryParse(x, out var y) ? (true, y) : (false, Min);
    protected override float Min => int.MinValue;
    protected override float Max => int.MaxValue;
}

public static class RangeConverterUtils
{
    private static readonly Dictionary<string, Res<(object, object)>> _fieldLut = [];

    public static Res<object>? PerformFull<T>(object? value, object? parameter, Func<string,(bool,T)> converter, T minDef, T maxDef) where T: IComparable
    {
        if (value is null) return null;
        if (value is not string strValue)
        {
            return ResC.TFail<object>("Failed to get input string", ResMsgLvl.Error);
        }

        var converted = converter(strValue);
        if (!converted.Item1)
        {
            return ResC.TFail<object>($"Provided value is not of type {typeof(T).Name}", ResMsgLvl.Error);
        }

        var res = FindRange(parameter, minDef, maxDef);
        if (!res.IsOk)
        {
            return ResC.TFail<object>(res.Msg);
        }

        if (converted.Item2.CompareTo(res.Value.Min) < 0 || converted.Item2.CompareTo(res.Value.Max) > 0)
        {
            return ResC.TFail<object>($"Provided value should be between {res.Value.Min} and {res.Value.Max}", ResMsgLvl.Error);
        }
        return ResC.TOk<object>(converted.Item2);
    }

    public static Res<(T Min, T Max)> FindRange<T>(object? parameter, T minDef, T maxDef) where T: IComparable
    {
        if (parameter is null)
        {
            return ResC.TOk((minDef, maxDef));
        }

        var findRes = FindFieldsRawLookup(parameter, minDef, maxDef);
        if (!findRes.IsOk)
        {
            return ResC.TFail<(T, T)>(findRes.Msg);
        }

        if (findRes.Value.Min is not T convMin)
        {
            return ResC.TFail<(T, T)>($"Failed to convert minimum to {typeof(T).Name}", ResMsgLvl.Error);
        }
        if (findRes.Value.Max is not T convMax)
        {
            return ResC.TFail<(T, T)>($"Failed to convert maximum to {typeof(T).Name}", ResMsgLvl.Error);
        }

        return ResC.TOk((convMin, convMax));
    }

    private static Res<(object Min, object Max)> FindFieldsRawLookup(object parameter, object minDef, object maxDef)
    {
        if (parameter is not string key)
        {
            return ResC.TFail<(object, object)>("Failed to get parameter string", ResMsgLvl.Error);
        }

        if (_fieldLut.TryGetValue(key, out var res))
        {
            return res;
        }

        var resFind = FindFieldsRawInner(key, minDef, maxDef);
        _fieldLut[key] = resFind;
        return resFind;
    }

    private static Res<(object Min, object Max)> FindFieldsRawInner(string key, object minDef, object maxDef)
    {
        var keySplit = key.Split('.');
        if (keySplit.Length != 2)
        {
            return ResC.TFail<(object, object)>("Failed to split parameter string", ResMsgLvl.Error);
        }

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic)
            .SelectMany(x => x.GetTypes())
            .Where(x => x.Name == keySplit[0]);

        foreach (var type in types)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(x => x.IsLiteral && !x.IsInitOnly);
            var fieldMin = fields.FirstOrDefault(x => x.Name == $"MIN_{keySplit[1]}");
            var fieldMax = fields.FirstOrDefault(x => x.Name == $"MAX_{keySplit[1]}");

            if (fieldMax != null || fieldMin != null)
            {
                return ExtractFromFields(fieldMin, fieldMax, minDef, maxDef);
            }
        }

        return ResC.TFail<(object, object)>("Failed to get parameters", ResMsgLvl.Error);
    }

    private static Res<(object Min, object Max)> ExtractFromFields(FieldInfo? fieldMin, FieldInfo? fieldMax, object minDef, object maxDef)
    {
        object minValue;
        object maxValue;

        if (fieldMin != null)
        {
            var minValueRaw = fieldMin.GetValue(null);
            if (minValueRaw is null)
            {
                return ResC.TFail<(object, object)>("Minimum parameter has no value", ResMsgLvl.Error);
            }
            minValue = minValueRaw;
        }
        else
        {
            minValue = minDef;
        }

        if (fieldMax != null)
        {
            var maxValueRaw = fieldMax.GetValue(null);
            if (maxValueRaw is null)
            {
                return ResC.TFail<(object, object)>("Maximum parameter has no value", ResMsgLvl.Error);
            }
            maxValue = maxValueRaw;
        }
        else
        {
            maxValue = maxDef;
        }

        return ResC.TOk((minValue, maxValue));
    }
}