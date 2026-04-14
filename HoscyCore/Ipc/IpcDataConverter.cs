using System.Text;
using HoscyCore.Utility;
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

    public Res<string> DeserializeBase64(string input)
    {
        return ResC.TWrap(() =>
        {
            var bytes = Convert.FromBase64String(input);
            return ResC.TOk(Encoding.UTF8.GetString(bytes));
        }, "Failed to decode message from Base64", _logger, ResMsgLvl.Warning);
    }

    public Res<T> DeserializeJson<T>(string input) where T : class
    {
        try
        {
            var substr = input[2..];
            var deserialized = JsonConvert.DeserializeObject<T>(substr);
            if (deserialized is null)
            {
                var type = typeof(T);
                _logger.Warning("DataConverter: Failed to deserialize from data {input} to type {type}, no output",
                    input, type.FullName);
                return ResC.TFail<T>(ResMsg.Wrn($"DataConverter: Failed to deserialize from data {input} to type {type.Name}, no output"));
            }
            return ResC.TOk(deserialized);
        }
        catch (Exception ex)
        {
            var type = typeof(T);
            _logger.Warning(ex, "DataConverter: Failed to deserialize from data {input} to type {type}",
                input, type.FullName);
            return ResC.TFail<T>(ResMsg.Wrn(ResMsg.FmtEx(ex, $"DataConverter: Failed to deserialize from data {input} to type {type.Name}")));
        }
    }

    public Res<string> Serialize<T>(char id, T input) where T : class
    {
        try
        {
            return ResC.TOk(SeralizeRaw(id, input));
        }
        catch (Exception ex)
        {
            var type = typeof(T);
            _logger.Warning(ex, "DataConverter: Failed to serialize data of type {type} with id {id}",
                type.FullName, id);
            return ResC.TFail<string>(ResMsg.Wrn(ResMsg.FmtEx(ex, $"DataConverter: Failed to serialize data of type {typeof(T).Name} with id {id}")));
        }
    }
    public static string SeralizeRaw<T>(char id, T input) where T : class
    {
        var json = JsonConvert.SerializeObject(input, Formatting.None);
        var base64Bytes = Encoding.UTF8.GetBytes($"{id} {json}");
        var base64String = Convert.ToBase64String(base64Bytes);
        return base64String;
    }
}