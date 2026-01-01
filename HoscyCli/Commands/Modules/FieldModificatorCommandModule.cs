using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using Serilog.Events;
using System.Globalization;
using System.Reflection;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(FieldModificatorCommandModule), Lifetime.Singleton)]
public class FieldModificatorCommandModule(ConfigModel config) : AttributeCommandModule
{
    private readonly ConfigModel _config = config;

    [SubCommandModule(["set", "s", "="], "Set a variable")]
    public CommandResult SetProperty(string? nameAndValue)
    {
        if (string.IsNullOrWhiteSpace(nameAndValue)) return CommandResult.MissingParameter;
        var (name, value) = Util.SplitAtFirstSpace(nameAndValue);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);

        if (!IsSimpleType(info.PropertyType))
        {
            Console.WriteLine($"Unable to directly set type {info.PropertyType.Name}, please use editor instead");
            return CommandResult.Error;
        }
        SetValue(info, _config, value); //todo: does this work?
        DisplayFieldInfo(info, _config);       
        return CommandResult.Success;
    }

    [SubCommandModule(["get", "g", ">"], "Get a variable value")] 
    public CommandResult GetProperty(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        if (!IsSimpleType(info.PropertyType))
        {
            Console.WriteLine($"Unable to directly view type {info.PropertyType.Name}, please use editor instead");
            return CommandResult.Error;
        }
        DisplayFieldInfo(info, _config);
        return CommandResult.Success;
    }

    private void DisplayFieldInfo(PropertyInfo info, object targetObj)
    {
        Console.WriteLine($"Field {info.Name} is of type {info.PropertyType.Name} and is currently set to {info.GetValue(targetObj)}");
        if (info.PropertyType.IsEnum)
        {
            Console.WriteLine($"Possible values: {string.Join(", ", Enum.GetNames(info.PropertyType))}");
        }
    }

    public void DeconstructComplex(object obj)
    {
        var type = obj.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //todo: ?
    }

    public void SetValue(PropertyInfo info, object targetObj, string stringValue)
    {
        var value = ParseValueString(stringValue, info.PropertyType);
        info.SetValue(targetObj, value);
    }

    private static readonly Dictionary<Type, Func<string, object>> _simpleTypeConverters = new()
    {
        {typeof(string),        (s) => s},
        {typeof(int),           (s) => int.Parse(s, NumberStyles.Any)},
        {typeof(bool),          (s) => ConvertBool(s)},
        {typeof(float),         (s) => float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)},
        {typeof(LogEventLevel), (s) => ConvertEnum<LogEventLevel>(s)},
        {typeof(ushort),        (s) => ushort.Parse(s, NumberStyles.Any)}
    };

    private static bool IsSimpleType(Type t)
    {
        return _simpleTypeConverters.ContainsKey(t);
    }

    private static object ParseValueString(string value, Type targetType)
    {
        return _simpleTypeConverters[targetType].Invoke(value);
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

    private PropertyInfo GetPropertyInfo(string name)
    {
        var match = _config.GetType().GetProperty(name);
        return match ?? throw new ArgumentException(name);
    }
}

/// Todo: Redesign of vairable setting
/// Following should be handled:
/// all methods should always take and get / set the direct config value on top level
/// base types should be handled instantly, complex values should be split up and then handled and then finally list/sets/dicts also need their editor
/// if possible current states should always get a readout so there should not only be a "set/get" but also "editor" system
/// preferably the whole system should be fairly independent of the actual type