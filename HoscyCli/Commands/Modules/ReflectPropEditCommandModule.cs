using System.Globalization;
using System.Reflection;
using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.DependencyCore;
using Serilog.Events;

namespace HoscyCli.Commands.Modules;

[LoadIntoDiContainer(typeof(ReflectPropEditCommandModule))]
public class ReflectPropEditCommandModule(ConfigModel config) : AttributeCommandModule
{
    private readonly ConfigModel _config = config;

    #region Entrypoint
    [SubCommandModule(["get", "g", ">"], "Get a variable")]
    public CommandResult GetProperty(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        HandleType(info.PropertyType,
        () => DisplaySimpleInfo(info.PropertyType, info.GetValue(_config)!, info.Name),
        () =>
            {
                var complexProps = info.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead).ToArray();
                Console.WriteLine($"Contents of complex property {info.Name} of type {info.PropertyType.Name}:\n{GenerateComplexList(complexProps, info.GetValue(_config)!)}");
            },
            () => Console.WriteLine($"Collection {info.Name} can only be viewed in editor")
        );
        return CommandResult.Success;
    }

    [SubCommandModule(["set", "s", "<"], "Set a variable")]
    public CommandResult SetProperty(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return CommandResult.MissingParameter;
        var info = GetPropertyInfo(name);
        HandleType(info.PropertyType,
            () =>
            {
                info.SetValue(_config, AskForSimpleTypeValue(info.PropertyType, info.GetValue(_config)!, info.Name));
                DisplaySimpleInfo(info.PropertyType, info.GetValue(_config)!, info.Name);
            },
            () => OpenComplexEditor(info.GetValue(_config)!, info.Name),
            () => OpenCollectionEditor(info.GetValue(_config)!, info.Name)
        );
        return CommandResult.Success;
    }

    private PropertyInfo GetPropertyInfo(string name)
    {
        var match = _config.GetType().GetProperty(name) ?? throw new ArgumentException($"Unable to find property {name}");
        if (!match.CanRead) throw new ArgumentException($"Property {name} is not readable");
        return match;
    }
    #endregion

    #region Selection
    public static void HandleType(Type type, Action onSimple, Action onComplex, Action onCollection)
    {
        if (IsSimpleType(type))
        {
            onSimple();
        } 
        else if (type.IsGenericType && type.GetInterface(nameof(IEnumerable<>)) is not null)
        {
            onCollection();
        } 
        else
        {
            onComplex();
        }
    }
    #endregion

    #region Simple Types
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

    private static object ParseSimpleTypeString(string value, Type targetType)
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

    private static object AskForSimpleTypeValue(Type simpleType, object currentValue, string fieldName)
    {
        while (true)
        {
            DisplaySimpleInfo(simpleType, currentValue, fieldName);
            Console.Write("New value / '!exit' > ");
            var input = Console.ReadLine();
            if (input == "!exit") break;
            if (input is null) continue;

            try
            {
                return ParseSimpleTypeString(input, simpleType);
            }
            catch (Exception e)
            {
                Util.DisplayEx(e);
            }
        }
        return currentValue;
    }

    private static void DisplaySimpleInfo(Type simpleType, object currentValue, string fieldName)
    {
        Console.WriteLine($"Field {fieldName} is of type {simpleType.Name} and is currently set to {currentValue}");
        if (simpleType.IsEnum)
        {
            Console.WriteLine($"Possible values: {string.Join(", ", Enum.GetNames(simpleType))}");
        }
    }
    #endregion

    #region Complex Types
    private static void OpenComplexEditor(object complexObj, string id)
    {
        var complexType = complexObj.GetType();
        var complexProps = complexType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanRead).ToArray();
        if (complexProps.Length == 0) throw new ArgumentException($"Complex property {complexType.Name} has no readable properties");

        while (true)
        {
            Console.Write($"\nEditing complex value {id} of type {complexType.Name}\n\nCurrent values:\n{GenerateComplexList(complexProps, complexObj)}\n\nNr to edit / '!exit' > ");
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

            //todo: test, fixup?
            HandleType(selected.PropertyType,
                () => selected.SetValue(complexObj, AskForSimpleTypeValue(selected.PropertyType, selected.GetValue(complexObj)!, selected.Name)),
                () => OpenComplexEditor(selected.GetValue(complexObj)!, selected.Name),
                () => OpenCollectionEditor(selected.GetValue(complexObj)!, selected.Name)
            );
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
    #endregion

    #region Collections
    private static readonly Dictionary<Guid, string> _collectionTypeConverters = new()
    {
        {typeof(IList<>).GUID, nameof(OpenListEditor)},
        {typeof(ISet<>).GUID, nameof(OpenSetEditor)},
        {typeof(IDictionary<,>).GUID, nameof(OpenDictEditor)}
    };

    private static void OpenCollectionEditor(object collectionObj, string id)
    {
        var collectionType = collectionObj.GetType();
        var genericPropType = collectionType.IsConstructedGenericType ? collectionType.GetGenericTypeDefinition() : collectionType;
        var matchingType = genericPropType.GetInterfaces().FirstOrDefault(x => _collectionTypeConverters.ContainsKey(x.GUID))
            ?? throw new ArgumentException($"Can not convert field {id} into an editable collection");

        //Get correct method
        var methodName = _collectionTypeConverters[matchingType.GUID];
        var method = typeof(ReflectPropEditCommandModule).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new ArgumentException($"Unable to find menu menu method {methodName} for field {id}");

        //Parameter check
        var methodParams = method.GetParameters();
        if (methodParams.Length != 2 || methodParams[0].ParameterType.GUID != matchingType.GUID || methodParams[1].ParameterType.GUID != typeof(string).GUID) 
            throw new ArgumentException($"Parameter for method does not match");

        //Invoke the method
        method.MakeGenericMethod(collectionType.GenericTypeArguments).Invoke(null, [collectionObj, id]);
    }

    private static void OpenListEditor<T>(IList<T> list, string id) //todo: refactor
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "Unknown List";
        }
        bool canWrite = list is IReadOnlyList<T>;

        while (true)
        {
            var i = 0;
            var listElementString = string.Join("\n", list.Select(x => $" {i++} - {x}"));

            Console.Write($"\nEditing list {id}\n\nCurrent values:\n{listElementString}\n\n's' to select / 'm' m to move / 'i' to insert at / 'r' to remove / '!exit'\n> ");
            var command = ParseCollectionCommand(Console.ReadLine() ?? string.Empty);
            if (command == CollectionCommand.Unknown)
            {
                Console.WriteLine("Please enter a valid command (Press any key)");
                Console.ReadKey();
                continue;   
            }
            if (command == CollectionCommand.Exit) break;
            Console.Write("Position? > ");
            if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 0 || idx >= list.Count + 1)
            {
                Console.WriteLine("Input value is not a valid number (Press any key)");
                Console.ReadKey();
                continue;
            }
            switch(command)
            {
                case CollectionCommand.Select:
                {
                    idx = int.Min(idx, list.Count - 1);

                    var idSub = $"List element {idx}";
                    HandleType(typeof(T),
                    () => list[idx] = (T)AskForSimpleTypeValue(typeof(T), list[idx]!, idSub),
                    () => OpenComplexEditor(list[idx]!, idSub),
                    () => OpenCollectionEditor(list[idx]!, idSub)
                    );
                    break;
                }
                case CollectionCommand.Move:
                {
                    Console.Write("At? > ");
                    if (!int.TryParse(Console.ReadLine(), out var idx2) || idx2 < 0 || idx2 >= list.Count + 1)
                    {
                        Console.WriteLine("Input value is not a valid number (Press any key)");
                        Console.ReadKey();
                        continue;
                    }
                    var item = list[int.Min(idx, list.Count - 1)];
                    list.RemoveAt(int.Min(idx, list.Count - 1));
                    list.Insert(idx2-1, item);
                    break;
                }
                case CollectionCommand.Remove:
                {
                    list.RemoveAt(int.Min(idx, list.Count - 1));
                    break;
                }
                case CollectionCommand.Insert:
                {
                    var newInstance = Activator.CreateInstance<T>();
                    if (newInstance is null)
                    {
                        Console.WriteLine($"New instance of class {typeof(T).Name} could not be created");
                        Console.ReadKey();
                    }
                    list.Insert(idx, newInstance);
                    break;
                }
            }
        }
    }

    private static void OpenDictEditor<T1,T2>(IDictionary<T1,T2> doct, string id)
    {
        Console.WriteLine("dict");
    }

    private static void OpenSetEditor<T>(ISet<T> set, string id)
    {
        Console.WriteLine("set");
    }

    private static CollectionCommand ParseCollectionCommand(string value)
    {
        return value.ToLowerInvariant() switch {
            "!exit" => CollectionCommand.Exit,
            "s" => CollectionCommand.Select,
            "m" => CollectionCommand.Move,
            "r" => CollectionCommand.Remove,
            "i" => CollectionCommand.Insert,
            _ => CollectionCommand.Unknown
        };
    }

    private enum CollectionCommand
    {
        Select,
        Remove,
        Insert,
        Move,
        Exit,
        Unknown
    }
    #endregion
}