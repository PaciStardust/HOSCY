using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Serilog;

namespace HoscyCore.Ipc;

public class IpcDataConverter(ILogger logger)
{
    private readonly ILogger _logger = logger;

    public bool IsValid(string input)
    {
        return input.Length > 3;
    } 

    public char GetIdentifier(string input)
    {
        return input[0];
    }

    public bool TryDeserialize<T>(string input, [NotNullWhen(true)] out T? deserialized) where T : class
    {
        try
        {
            var substr = input[2..];
            deserialized = JsonConvert.DeserializeObject<T>(substr);
            if (deserialized is null)
            {
                _logger.Warning("DataConverter: Failed to deserialize from data {data} to type {type}, no output",
                    input, typeof(T).Name);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "DataConverter: Failed to deserialize from data {data} to type {type}",
                input, typeof(T).Name);
            deserialized = null;
            return false;
        }
    }

    public bool TrySerialize<T>(char id, T input, [NotNullWhen(true)] out string? serialized) where T : class
    {
        try
        {
            serialized = SeralizeRaw(id, input);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "DataConverter: Failed to serialize data of type {type} with id {id}",
                typeof(T).Name, id);
            serialized = null;
            return false;
        }
    }
    public static string SeralizeRaw<T>(char id, T input) where T : class
    {
        var json = JsonConvert.SerializeObject(input, Formatting.None);
        return $"{id} {json}";
    }
}