using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using Serilog.Events;
using System.Collections;
using System.Globalization;
using System.Reflection;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(FieldModificatorCommandModule), Lifetime.Singleton)]
public class FieldModificatorCommandModule(ConfigModel config) : AttributeCommandModule
{
    private readonly ConfigModel _config = config;

    [SubCommandModule(["set", "s", "="], "Set a variable")] //todo: complex not error
    public CommandResult SetProperty(string? nameAndValue)
    {
        if (string.IsNullOrWhiteSpace(nameAndValue)) return CommandResult.MissingParameter;
        var (name, value) = Util.SplitAtFirstSpace(nameAndValue);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        if (!info.CanWrite)
        {
            Console.WriteLine($"Unable to set type {info.PropertyType.Name}, it is readonly");
            return CommandResult.Error;
        }

        if (!IsSimpleType(info.PropertyType))
        {
            Console.WriteLine($"Unable to directly set type {info.PropertyType.Name}, please use editor instead");
            return CommandResult.Error;
        }
        SetValue(info, _config, value);
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

    [SubCommandModule(["editor", "e", "*"], "Edit a variable value")] 
    public CommandResult EditProperty(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        if (IsSimpleType(info.PropertyType))
        {
            AskForValue(info, _config);
        }
        else if (info.PropertyType.IsGenericType && info.PropertyType.GetInterface(nameof(IEnumerable<>)) is not null)
        {
            AskForEnumerable(info, _config);
        } 
        else
        {
            AskForComplexProperty(info, _config);
        }
        return CommandResult.Success;
    }

    private static readonly Dictionary<Guid, string> _collectionTypeConverters = new()
    {
        {typeof(IList<>).GUID, nameof(AskForList)},
        {typeof(ISet<>).GUID, nameof(AskForSet)},
        {typeof(IDictionary<,>).GUID, nameof(AskForDict)}
    };

    private static void AskForEnumerable(PropertyInfo info, object targetObj)
    {
        var collectionObj = info.GetValue(targetObj) ?? throw new ArgumentException($"Unable to retrieve field {info.Name}");
        var genericPropType = info.PropertyType.IsConstructedGenericType ? info.PropertyType.GetGenericTypeDefinition() : info.PropertyType;
        var matchingType = genericPropType.GetInterfaces().FirstOrDefault(x => _collectionTypeConverters.ContainsKey(x.GUID))
            ?? throw new ArgumentException($"Can not convert field {info.Name} into an editable collection");

        //Get correct method
        var methodName = _collectionTypeConverters[matchingType.GUID];
        var method = typeof(FieldModificatorCommandModule).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Unable to find menu menu method {methodName} for field {info.Name}");

        //Parameter check
        var methodParams = method.GetParameters();
        if (methodParams.Length != 1 || methodParams[0].ParameterType.GUID != matchingType.GUID) 
            throw new ArgumentException($"Parameter for method does not match");

        //Invoke the method
        method.MakeGenericMethod(info.PropertyType.GenericTypeArguments).Invoke(null, [collectionObj]);
    }

    private static void AskForList<T>(IList<T> list)
    {
        Console.WriteLine("list");
    }

    private static void AskForDict<T1,T2>(IDictionary<T1,T2> list)
    {
        Console.WriteLine("dict");
    }

    private static void AskForSet<T>(ISet<T> set)
    {
        Console.WriteLine("set");
    }

    private static void AskForValue(PropertyInfo info, object targetObj)
    {
        while (true)
        {
            DisplayFieldInfo(info, targetObj);
            Console.Write("New value / '!exit' > ");
            var input = Console.ReadLine();
            if (input == "!exit") break;
            if (input is null) continue;

            try
            {
                SetValue(info, targetObj, input);
                DisplayFieldInfo(info, targetObj);
                break;
            }
            catch (Exception e)
            {
                Util.DisplayEx(e);
            }
        }
    }

    private static void AskForComplexProperty(PropertyInfo complexInfo, object parentObj)
    {
        var complexProps = complexInfo.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead).ToArray();
        if (complexProps.Length == 0) throw new ArgumentException($"Complex property {complexInfo.PropertyType.Name} has no readable properties");
        var currentComplex = complexInfo.GetValue(parentObj)!;

        while (true)
        {
            Console.Write($"\nEditing complex value {complexInfo.Name}\n\nCurrent values:\n{GenerateComplexList(complexProps, currentComplex)}\n\nNr to edit / '!exit' > ");
            var input = Console.ReadLine();
            if (input == "!exit") break;
            if (input is null) continue;
            if (!int.TryParse(input, out var idx) || idx < 0 || idx >= complexProps.Length)
            {
                Console.WriteLine("Input value is not a valid number (Press any key)");
                Console.ReadKey();
                continue;
            }
            var selected = complexProps[idx];
            if (!selected.CanWrite)
            {
                Console.WriteLine("Value is readonly (Press any key)");
                Console.ReadKey();
                continue;
            }
            AskForValue(selected, currentComplex);
        }
    }

    private static string GenerateComplexList(PropertyInfo[] infos, object targetObj)
    {
        var i = 0;
        return string.Join("\n", infos.Select(x =>
        {
            var value = IsSimpleType(x.PropertyType) ? x.GetValue(targetObj)?.ToString() ?? "[NULL]" : "[COMPLEX]";
            return $" {i++} - {(x.CanWrite ? string.Empty : "[R] ")}{x.Name} ({x.PropertyType.Name}): {value}";
        }));
    }

    private static void DisplayFieldInfo(PropertyInfo info, object targetObj)
    {
        Console.WriteLine($"Field {info.Name} is of type {info.PropertyType.Name} and is currently set to {info.GetValue(targetObj)}");
        if (info.PropertyType.IsEnum)
        {
            Console.WriteLine($"Possible values: {string.Join(", ", Enum.GetNames(info.PropertyType))}");
        }
    }

    public static void SetValue(PropertyInfo info, object targetObj, string stringValue)
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
        var match = _config.GetType().GetProperty(name) ?? throw new ArgumentException($"Unable to find property {name}");
        if (!match.CanRead) throw new ArgumentException($"Property {name} is not readable");
        return match;
    }
}

/// Todo: Redesign of vairable setting
/// Following should be handled:
/// all methods should always take and get / set the direct config value on top level
/// base types should be handled instantly, complex values should be split up and then handled and then finally list/sets/dicts also need their editor
/// if possible current states should always get a readout so there should not only be a "set/get" but also "editor" system
/// preferably the whole system should be fairly independent of the actual type