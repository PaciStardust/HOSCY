using System.Globalization;
using System.Reflection;
using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using Serilog.Events;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(SimpleVariableCommandModule), Lifetime.Singleton)]
public class SimpleVariableCommandModule(ConfigModel config) : AttributeCommandModule
{
    private readonly ConfigModel _config = config;

    [SubCommandModule(["set", "s", "="], "Set a variable")]
    public CommandResult SetProperty(string? nameAndValue)
    {
        if (string.IsNullOrWhiteSpace(nameAndValue)) return CommandResult.MissingParameter;
        var (name, value) = Util.SplitAtFirstSpace(nameAndValue);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        object parsedValue = ParseValueString(value, info.PropertyType);
        info.SetValue(_config, parsedValue);
        DisplayVariableInfo(info);       
        return CommandResult.Success;
    }

    [SubCommandModule(["get", "g", ">"], "Get a variable value")] 
    public CommandResult GetProperty(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        DisplayVariableInfo(info);
        return CommandResult.Success;
    }

    private void DisplayVariableInfo(PropertyInfo info)
    {
        Console.WriteLine($"Variable {info.Name} is of type {info.PropertyType.Name} and is currently set to {info.GetValue(_config)}");
        if (info.PropertyType.IsEnum)
        {
            Console.WriteLine($"Possible values: {string.Join(", ", Enum.GetNames(info.PropertyType))}");
        }
    }

    private PropertyInfo GetPropertyInfo(string name)
    {
        var match = _config.GetType().GetProperty(name);
        return match ?? throw new ArgumentException(name);
    }

    private object ParseValueString(string value, Type targetType)
    {
        if (targetType == typeof(string))           return value;
        if (targetType == typeof(int))              return int.Parse(value, NumberStyles.Any);
        if (targetType == typeof(bool))             return ConvertBool(value);
        if (targetType == typeof(float))            return float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
        if (targetType == typeof(LogEventLevel))    return ConvertEnum<LogEventLevel>(value);
        if (targetType == typeof(ushort))           return ushort.Parse(value, NumberStyles.Any);

        throw new NotImplementedException($"Conversion to {targetType.FullName} not implemented");
    }   

    private static readonly string[] _trueStrings = ["true", "t", "1", "yes", "on"];
    private static readonly string[] _falseStrings = ["false", "f", "0", "no", "off"];
    public static bool ConvertBool(string value)
    {
        value = value.ToLower();
        if (_trueStrings.Contains(value)) return true;
        if (_falseStrings.Contains(value)) return false;
        throw new ArgumentException($"Value {value} can not be converted to bool");
    }

    private static T ConvertEnum<T>(string value) where T : Enum {
        try
        {
            if (int.TryParse(value, out var convInt))
                return (T)(object)convInt;
            return (T)Enum.Parse(typeof(T), value);
        }
        catch
        {
            Console.WriteLine($"Failed to convert to enum {typeof(T).FullName}, possible values: {string.Join(", ", Enum.GetNames(typeof(T)))}");
            throw;
        }
    }
}