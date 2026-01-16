using System.Collections.Frozen;
using System.Collections.ObjectModel;
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
    private const string EMPTY_INDICATOR = "[EMPTY]";

    #region Entrypoint
    [SubCommandModule(["get", "g", ">"], "Get a variable")]
    public CommandResult GetProperty(string? name)
    {
        if (!IsNotEmpty(name, "Variable to retrieve must be specified")) return CommandResult.MissingParameter;
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

    [SubCommandModule(["set", "s", "<", "edit", "e"], "Set a variable")]
    public CommandResult SetProperty(string? name)
    {
        if (!IsNotEmpty(name, "Variable to edit must be specified")) return CommandResult.MissingParameter;
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
    private static void HandleType(Type type, Action onSimple, Action onComplex, Action onCollection)
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
    private static readonly FrozenDictionary<Type, Func<string, object>> _simpleTypeConverters = new Dictionary<Type, Func<string, object>>()
    {
        {typeof(string),        (s) => s},
        {typeof(int),           (s) => int.Parse(s, NumberStyles.Any)},
        {typeof(bool),          (s) => ConvertBool(s)},
        {typeof(float),         (s) => float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture)},
        {typeof(LogEventLevel), (s) => ConvertEnum<LogEventLevel>(s)},
        {typeof(ushort),        (s) => ushort.Parse(s, NumberStyles.Any)}
    }.ToFrozenDictionary();

    private static bool IsSimpleType(Type t)
    {
        return _simpleTypeConverters.ContainsKey(t);
    }

    private static object ParseSimpleTypeString(string value, Type targetType)
    {
        return _simpleTypeConverters[targetType].Invoke(value);
    }   

    private static readonly FrozenSet<string> _trueStrings = ["true", "t", "1", "yes", "on"];
    private static readonly FrozenSet<string> _falseStrings = ["false", "f", "0", "no", "off"];
    private static bool ConvertBool(string value)
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
                ReadKeyError("Input value is not a valid number");
                continue;
            }
            var selected = complexProps[idx];
            if (!selected.CanWrite)
            {
                ReadKeyError("Value is readonly");
                continue;
            }

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
        var retString = string.Join("\n", infos.Select(x =>
        {
            var value = IsSimpleType(x.PropertyType) ? x.GetValue(targetObj)?.ToString() ?? "[NULL]" : "[COMPLEX]";
            return $" {i++} - {(x.CanWrite ? string.Empty : "[R] ")}{x.Name} ({x.PropertyType.Name}): {value}";
        }));
        return string.IsNullOrWhiteSpace(retString) ? EMPTY_INDICATOR : retString;
    }
    #endregion

    #region Collections
    private static readonly FrozenDictionary<Guid, string> _collectionTypeConverters = new Dictionary<Guid, string>()
    {
        {typeof(IList<>).GUID, nameof(OpenListEditor)},
        {typeof(ISet<>).GUID, nameof(OpenSetEditor)},
        {typeof(IDictionary<,>).GUID, nameof(OpenDictEditor)}
    }.ToFrozenDictionary();

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

    private static void OpenListEditor<T>(IList<T> list, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "Unknown List";
        }
        bool canWrite = list is IReadOnlyList<T>;
        CollectionCommand[] allowedCommands = canWrite 
            ? [CollectionCommand.Select, CollectionCommand.Move, CollectionCommand.Insert, CollectionCommand.Remove]
            : [CollectionCommand.Select];
        var commandString = GetCommandString(allowedCommands);

        while (true)
        {
            var listElementIndex = 0;
            var listElementString = string.Join("\n", list.Select(x => $" {listElementIndex++} - {x}"));
            if (string.IsNullOrWhiteSpace(listElementString))
            {
                listElementString = EMPTY_INDICATOR;
            }

            var command = AskForCollectionCommand(id, listElementString, commandString, allowedCommands);
            if (command == CollectionCommand.Unknown)
            {
                ReadKeyError("Please enter a valid command");
                continue;   
            }
            if (command == CollectionCommand.Exit) break;

            var pos = AskForIndex("Position?", list.Count);
            if (pos is null) continue;
            
            HandleCommand(command,
                onSelect: () =>
                {
                    var idSub = $"List element {pos.Value.IndexLimit}";
                    HandleType(typeof(T),
                        onSimple: () => list[pos.Value.IndexLimit] = (T)AskForSimpleTypeValue(typeof(T), list[pos.Value.IndexLimit]!, idSub),
                        onComplex: () => OpenComplexEditor(list[pos.Value.IndexLimit]!, idSub),
                        onCollection: () => OpenCollectionEditor(list[pos.Value.IndexLimit]!, idSub)
                    );
                },
                onMove: () =>
                {
                    var pos2 = AskForIndex("At?", list.Count);
                    if (pos2 is null) return;
                    var item = list[pos.Value.IndexLimit];
                    list.RemoveAt(pos.Value.IndexLimit);
                    list.Insert(pos2.Value.PlusOneIndexLimit, item);
                },
                onRemove: () =>
                {
                    list.RemoveAt(pos.Value.IndexLimit);
                },
                onInsert: () =>
                {
                    var newInstance = ActivatorCreateInstance<T>();
                    if (newInstance is null)
                    {
                        ReadKeyError($"New instance of class {typeof(T).Name} could not be created");
                        return;
                    }
                    list.Insert(pos.Value.PlusOneIndexLimit, newInstance);
                }
            );
        }
    }

    private static void OpenDictEditor<T1,T2>(IDictionary<T1,T2> dict, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "Unknown Dictionary";
        }
        bool canWrite = dict is IReadOnlyDictionary<T1,T2>;
        CollectionCommand[] allowedCommands = canWrite 
            ? [CollectionCommand.Select, CollectionCommand.Insert, CollectionCommand.Remove]
            : [CollectionCommand.Select];
        var commandString = GetCommandString(allowedCommands);

        while (true)
        {
            var keyArray = dict.Keys.ToArray();
            var keyElementIndex = 0;
            var keyElementString = string.Join("\n", keyArray.Select(x => $" {keyElementIndex++} - {x} => {dict[x]}"));
            if (string.IsNullOrWhiteSpace(keyElementString))
            {
                keyElementString = EMPTY_INDICATOR;
            }

            var command = AskForCollectionCommand(id, keyElementString, commandString, allowedCommands);
            if (command == CollectionCommand.Unknown)
            {
                ReadKeyError("Please enter a valid command");
                continue;   
            }
            if (command == CollectionCommand.Exit) break;

            var pos = command != CollectionCommand.Insert ? AskForIndex("Position?", keyArray.Length) : (0,0);
            if (pos is null) continue;

            HandleCommand(command,
                onSelect: () =>
                {
                    var idSub = $"Dictionary element {keyArray[pos.Value.IndexLimit]}";
                    HandleType(typeof(T2),
                        onSimple: () => dict[keyArray[pos.Value.IndexLimit]] = (T2)AskForSimpleTypeValue(typeof(T2), dict[keyArray[pos.Value.IndexLimit]]!, idSub),
                        onComplex: () => OpenComplexEditor(dict[keyArray[pos.Value.IndexLimit]]!, idSub),
                        onCollection: () => OpenCollectionEditor(dict[keyArray[pos.Value.IndexLimit]]!, idSub)
                    );
                },
                onRemove: () =>
                {
                    dict.Remove(keyArray[pos.Value.IndexLimit]);
                },
                onInsert: () =>
                {
                    ReadKeyError("Configure Key");
                    var keyInstance = ActivatorCreateInstance<T1>();
                    if (keyInstance is null)
                    {
                        ReadKeyError($"New instance of class {typeof(T1).Name} could not be created");
                        return;
                    }
                    var keyId = "New key";
                    HandleType(typeof(T1),
                        onSimple: () => keyInstance = (T1)AskForSimpleTypeValue(typeof(T1), keyInstance!, keyId),
                        onComplex: () => OpenComplexEditor(keyInstance!, keyId),
                        onCollection: () => OpenCollectionEditor(keyInstance!, keyId)
                    );

                    ReadKeyError("Configure Value");
                    var valueInstance = ActivatorCreateInstance<T2>();
                    if (valueInstance is null)
                    {
                        ReadKeyError($"New instance of class {typeof(T1).Name} could not be created");
                        return;
                    }
                    var valueId = "New value";
                    HandleType(typeof(T2),
                        onSimple: () => valueInstance = (T2)AskForSimpleTypeValue(typeof(T2), valueInstance!, valueId),
                        onComplex: () => OpenComplexEditor(valueInstance!, valueId),
                        onCollection: () => OpenCollectionEditor(valueInstance!, valueId)
                    );

                    if (!dict.TryAdd(keyInstance, valueInstance))
                    {
                        ReadKeyError($"Key/Value pair could not be added to dictionary");
                    }
                }
            );
        }
    }

    private static void OpenSetEditor<T>(ISet<T> set, string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "Unknown Set";
        }
        bool canWrite = set is IReadOnlySet<T>;
        CollectionCommand[] allowedCommands = canWrite 
            ? [CollectionCommand.Insert, CollectionCommand.Remove]
            : [CollectionCommand.Select];
        var commandString = GetCommandString(allowedCommands);

        while (true)
        {
            var setArray = set.ToArray();
            var setArrayElementIndex = 0;
            var setArrayElementString = string.Join(separator: "\n", setArray.Select(x => $" {setArrayElementIndex++} - {x}"));
            if (string.IsNullOrWhiteSpace(setArrayElementString))
            {
                setArrayElementString = EMPTY_INDICATOR;
            }

            var command = AskForCollectionCommand(id, setArrayElementString, commandString, allowedCommands);
            if (command == CollectionCommand.Unknown)
            {
                ReadKeyError("Please enter a valid command");
                continue;   
            }
            if (command == CollectionCommand.Exit) break;

            var pos = command != CollectionCommand.Insert ? AskForIndex("Position?", setArray.Length) : (0,0);
            if (pos is null) continue;
            
            HandleCommand(command,
                onRemove: () =>
                {
                    set.Remove(setArray[pos.Value.IndexLimit]);
                },
                onInsert: () =>
                {
                    var newInstance = ActivatorCreateInstance<T>();
                    if (newInstance is null)
                    {
                        ReadKeyError($"New instance of class {typeof(T).Name} could not be created");
                        return;
                    }

                    var valueId = "Set item";
                    HandleType(typeof(T),
                        onSimple: () => newInstance = (T)AskForSimpleTypeValue(typeof(T), newInstance!, valueId),
                        onComplex: () => OpenComplexEditor(newInstance!, valueId),
                        onCollection: () => OpenCollectionEditor(newInstance!, valueId)
                    );

                    if (!set.Add(newInstance))
                    {
                        ReadKeyError($"Item could not be added to set");
                    }
                }
            );
        }
    }

    private static readonly FrozenDictionary<CollectionCommand, (string Parsed, string Desc)> _commandInfos = new Dictionary<CollectionCommand, (string Parsed, string Desc)>()
    {
        {CollectionCommand.Exit,      ("!exit", "to exit")},
        {CollectionCommand.Insert,    ("i", "to insert")},
        {CollectionCommand.Move,      ("m", "to move")},
        {CollectionCommand.Remove,    ("r", "to remove")},
        {CollectionCommand.Select,    ("s", "to select")}  
    }.ToFrozenDictionary();
    private static string GetCommandString(CollectionCommand[] availableCommands)
    {
        var cmdStrings = availableCommands.Distinct()
            .Where(x => _commandInfos.ContainsKey(x) && x != CollectionCommand.Exit && x != CollectionCommand.Unknown)
            .Select(x => { var (parsed, desc) = _commandInfos[x]; return $"{parsed} {desc}"; })
            .ToList();

        var (exitParsed, exitDesc) = _commandInfos[CollectionCommand.Exit];
        cmdStrings.Add($"{exitParsed} {exitDesc}");
        return string.Join(" / ", cmdStrings);
    }

    private static CollectionCommand AskForCollectionCommand(string collectionName, string currentValues, string commandString, CollectionCommand[] availableCommands)
    {
        Console.Write($"\nEditing {collectionName}\n\nCurrent values:\n{currentValues}\n\n{commandString}\n> ");
        var key = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(key)) return CollectionCommand.Unknown;

        var keyLower = key.ToLowerInvariant();
        var results = _commandInfos.Where(x => x.Value.Parsed == keyLower).ToArray();
        var actualResult = results.Length == 0 ? CollectionCommand.Unknown : results[0].Key;
        return actualResult != CollectionCommand.Exit && !availableCommands.Contains(actualResult)
            ? CollectionCommand.Unknown
            : actualResult;
    }

    private static (int IndexLimit, int PlusOneIndexLimit)? AskForIndex(string question, int length)
    {
        Console.Write($"{question} > ");
        if (!int.TryParse(Console.ReadLine(), out var idx) || idx < 0 || idx > length)
        {
            ReadKeyError("Input value is not a valid number");
            return null;
        }
        return (int.Min(idx, length-1), idx);
    }

    private static void HandleCommand(CollectionCommand command, Action? onSelect = null, Action? onRemove = null, Action? onInsert = null, Action? onMove = null)
    {
        switch(command)
        {
            case CollectionCommand.Select: onSelect?.Invoke(); break;
            case CollectionCommand.Remove: onRemove?.Invoke(); break;
            case CollectionCommand.Insert: onInsert?.Invoke(); break;
            case CollectionCommand.Move: onMove?.Invoke(); break;
        }
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

    #region Util
    private static void ReadKeyError(string message)
    {
        Console.WriteLine($"{message} (Press any key)");
        Console.ReadKey();
    }

    private static T ActivatorCreateInstance<T>()
    {
        if (typeof(T).IsAbstract || typeof(T).IsInterface) throw new ArgumentException("Cannot create an instance of abstract or interface type");
        if (typeof(T).GUID == typeof(string).GUID) return (T)(object)"New String";
        return Activator.CreateInstance<T>();
    }

    public static string[] GetAllConfigValues()
    {
        var props = typeof(ConfigModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return props?.Select(x => x.Name).ToArray() ?? [];
    }
    #endregion
}