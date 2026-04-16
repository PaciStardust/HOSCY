using System.Globalization;
using System.Net;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Osc.Query;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Osc.Command;

[LoadIntoDiContainer(typeof(IOscCommandParser))]
public class OscCommandParser(ILogger logger, OscQueryHostRegistry hosts) : IOscCommandParser
{
    private readonly ILogger _logger = logger.ForContext<OscCommandParser>();
    private readonly OscQueryHostRegistry _hosts = hosts;

    public const string OSC_COMMAND_IDENTIFIER = "[OSC]";

    public bool DetectCommandPrefix(string commandString)
    {
        return commandString.StartsWith(OSC_COMMAND_IDENTIFIER, StringComparison.OrdinalIgnoreCase);
    }

    public Res<OscCommandInfo[]> Parse(string commandString)
    {
        _logger.Debug("Parsing osc command string: {commandString}", commandString);
        return ResC.TWrap(() =>
        {
            var res = ParseInternal(commandString);
            if (res.IsOk)
            {
                _logger.Debug("Parsed osc command string: {commandString} => {cmdCount} commands",
                    commandString, res.Value.Length);
                return res;
            }
            else
            {
                return ResC.TFailLog<OscCommandInfo[]>(res.Msg.Message, _logger, lvl: res.Msg.Level);
            }
        }, $"Failed parsing OSC command {commandString}", _logger);
    }

    private Res<OscCommandInfo[]> ParseInternal(string commandString)
    {
        List<OscCommandInfo> parsedCommands = [];
        var index = OSC_COMMAND_IDENTIFIER.Length;

        // This detects specifically the start of a single command => [
        while(true)
        {
            var nextIndex = FindNextNonSpaceCharacter(ref commandString, index);
            if (nextIndex is null) break;
            index = nextIndex.Value;

            var currentChar = commandString[index];

            if (currentChar == '[')
            {
                var commandResult = ParseSingleCommand(ref commandString, index + 1);
                if (!commandResult.IsOk) return ResC.TFail<OscCommandInfo[]>(commandResult.Msg);

                index = commandResult.Value.NextIndex;
                parsedCommands.Add(commandResult.Value.Info);

                continue;
            }

            return ResC.TFail<OscCommandInfo[]>($"Unexpeced character '{currentChar}' between commands at index {index}");
        }

        return ResC.TOk(parsedCommands.ToArray());
    }

    private static int? FindNextNonSpaceCharacter(ref string commandString, int startIndex)
    {
        var index = startIndex;
        while (index < commandString.Length)
        {
            if (commandString[index] == ' ')
            {
                index++;
                continue;
            }
            return index;
        }
        return null;
    }

    private Res<(int NextIndex, OscCommandInfo Info)> ParseSingleCommand(ref string commandString, int startIndex)
    {
        var charIndex = FindNextNonSpaceCharacter(ref commandString, startIndex);
        if (charIndex is null) return ResC.TFail<(int, OscCommandInfo)>($"Failed to locate start of OSC address at index {startIndex}");

        var oscAddressResult = ParseOscAddress(ref commandString, charIndex.Value); 
        if (!oscAddressResult.IsOk) return ResC.TFail<(int, OscCommandInfo)>(oscAddressResult.Msg);

        charIndex = FindNextNonSpaceCharacter(ref commandString, oscAddressResult.Value.NextIndex);
        if (charIndex is null) return ResC.TFail<(int, OscCommandInfo)>($"Failed to locate start of parameters at index {oscAddressResult.Value.NextIndex}");

        var parameterResult = ParseParameters(ref commandString, charIndex.Value);
        if (!parameterResult.IsOk) return ResC.TFail<(int, OscCommandInfo)>(parameterResult.Msg);

        var targetAddressResult = ParseTargetAddress(ref commandString, parameterResult.Value.NextIndex);
        if (targetAddressResult is not null)
        {
            if (!targetAddressResult.IsOk) return ResC.TFail<(int, OscCommandInfo)>(targetAddressResult.Msg);

            charIndex = FindNextNonSpaceCharacter(ref commandString, targetAddressResult.Value.NextIndex);
            if (charIndex is null) return ResC.TFail<(int, OscCommandInfo)>($"Failed to locate next segment after target address at index {targetAddressResult.Value.NextIndex}");
        } 
        else
        {
            charIndex = parameterResult.Value.NextIndex;
        }

        var namedTargetResult = ParseNamedTarget(ref commandString, charIndex.Value);
        if (namedTargetResult is not null)
        {
            if (!namedTargetResult.IsOk) return ResC.TFail<(int, OscCommandInfo)>(namedTargetResult.Msg);

            charIndex = FindNextNonSpaceCharacter(ref commandString, namedTargetResult.Value.NextIndex);
            if (charIndex is null) return ResC.TFail<(int, OscCommandInfo)>($"Failed to locate next segment after named target at index {namedTargetResult.Value.NextIndex}");
        }

        var waitResult = ParseWait(ref commandString, charIndex.Value);
        if (waitResult is not null)
        {
            if (!waitResult.IsOk) return ResC.TFail<(int, OscCommandInfo)>(waitResult.Msg);

            charIndex = FindNextNonSpaceCharacter(ref commandString, waitResult.Value.NextIndex);
            if (charIndex is null) return ResC.TFail<(int, OscCommandInfo)>($"Failed to locate next segment after wait at index {waitResult.Value.NextIndex}");
        }

        if (commandString[charIndex.Value] != ']')
            return ResC.TFail<(int, OscCommandInfo)>($"Failed to locate end of OSC command at index {charIndex.Value}, it is either missing or contains unknown parameter");

        var commandInfoResult = CreateCommandInfo
        (
            oscAddressResult.Value.Address, 
            parameterResult.Value.Parameters,
            targetAddressResult?.Value.Ip,
            targetAddressResult?.Value.Port,
            namedTargetResult?.Value.Target,
            waitResult?.Value.Wait
        );

        if (!commandInfoResult.IsOk) 
            return ResC.TFail<(int, OscCommandInfo)>(commandInfoResult.Msg);
        return ResC.TOk((charIndex.Value + 1, commandInfoResult.Value));
    }

    private static readonly char[] _chars_alnum = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static Res<(int NextIndex, string Address)> ParseOscAddress(ref string commandString, int startIndex)
    {
        if (commandString[startIndex] != '/') 
            return ResC.TFail<(int, string)>($"First character of osc address at index {startIndex} should not be '{commandString[startIndex]}', always has to be '/'");

        var addressStartIndex = startIndex;
        var index = addressStartIndex + 1;

        var nextCanBeSlash = false;

        while (index < commandString.Length)
        {
            var currentChar = commandString[index];

            if (_chars_alnum.Contains(currentChar))
            {
                nextCanBeSlash = true;
                index++;
                continue;
            }

            if (currentChar == '/')
            {
                if (nextCanBeSlash)
                {
                    nextCanBeSlash = false;
                    index++;
                    continue;
                }
                return ResC.TFail<(int, string)>($"OSC Address invalid, character '/' repeated twice in a row at osc address index {index}");
            }

            if (currentChar == ' ')
            {
                if (index - addressStartIndex < 2)
                    return ResC.TFail<(int, string)>("OSC Address invalid, it can not be a singular slash");

                var length = nextCanBeSlash ? index - addressStartIndex : index - addressStartIndex - 1;
                return ResC.TOk((index + 1, commandString.Substring(addressStartIndex, length)));
            }

            return ResC.TFail<(int, string)>($"OSC Address invalid, character '{currentChar}' at index {index} is not allowed, address can only contain numbers, letters, and slashes");
        }

        return ResC.TFail<(int, string)>($"OSC Address invalid, search went until end of string");
    }

    private static Res<(int NextIndex, object[] Parameters)> ParseParameters(ref string commandString, int startIndex)
    {
        List<object> parameters = [];
        var index = startIndex;

        while (index < commandString.Length)
        {
            if (commandString[index] != '[') // Next segment can not be a command
                break;

            if (index + 3 > commandString.Length)
                return ResC.TFail<(int, object[])>($"Failed to find enough space in string where a parameter should be at index {index}");

            if (commandString[index + 2] != ']')
                return ResC.TFail<(int, object[])>($"Exptected end of parameter type indicator ']' at index {index}, got '{commandString[index + 2]} instead'");

            Res<(int NextIndex, object Parameter)> parameterRes = commandString[index + 1] switch
            {
                'b' or 'B' => ParseParameterBool(ref commandString, index + 3),
                's' or 'S' => ParseParameterString(ref commandString, index + 3),
                'i' or 'I' => ParseParameterInt(ref commandString, index + 3),
                'f' or 'F' => ParseParameterFloat(ref commandString, index + 3),
                _ => ResC.TFail<(int, object)>($"Unexpected argument type {commandString[index + 1]} at index {index + 1}")
            };

            if (!parameterRes.IsOk)
                return ResC.TFail<(int, object[])>(parameterRes.Msg);

            parameters.Add(parameterRes.Value.Parameter);

            var nextCharIndex = FindNextNonSpaceCharacter(ref commandString, parameterRes.Value.NextIndex);
            if (nextCharIndex is null)
                return ResC.TFail<(int, object[])>($"Failed to find next segment after argument at index {parameterRes.Value.NextIndex}");
            index = nextCharIndex.Value;
        }

        return ResC.TOk((index, parameters.ToArray()));
    }

    private static Res<(int NextIndex, object Parameter)> ParseParameterBool(ref string commandString, int startIndex)
    {
        var lastIndexExcl = startIndex;
        while (true)
        {
            if (commandString[lastIndexExcl] == ' ' || commandString[lastIndexExcl] == ']') break;
            lastIndexExcl++;

            if (lastIndexExcl >= commandString.Length)
                return ResC.TFail<(int, object)>($"Failed to find end of bool parameter at index {startIndex}");
        }

        if (lastIndexExcl == startIndex)
            return ResC.TFail<(int, object)>($"Failed to find value for bool parameter at index {startIndex}");

        var value = commandString[startIndex] switch // Only the first character matters
        {
            't' or 'T' or 'y' or 'Y' or '1' => ResC.TOk(true),
            'f' or 'F' or 'n' or 'N' or '0' => ResC.TOk(false),
            _ => ResC.TFail<bool>($"Value of bool parameter at index {startIndex} can not be inferred from text starting with '{commandString[startIndex]}'")
        };

        return value.IsOk ? ResC.TOk((lastIndexExcl, (object)value.Value)) : ResC.TFail<(int, object)>(value.Msg);
    }

    private static Res<(int NextIndex, object Parameter)> ParseParameterString(ref string commandString, int startIndex)
    {
        if (commandString[startIndex] != '"')
            return ResC.TFail<(int, object)>($"String parameter at index {startIndex} does not start with '\"'");

        var textStartIndex = startIndex + 1;
        var lastIndex = textStartIndex;
        var inEscape = false;

        while (true)
        {
            if (lastIndex >= commandString.Length)
                return ResC.TFail<(int, object)>($"Failed to find end of string parameter at index {startIndex}");

            var currentChar = commandString[lastIndex];
            if (!inEscape && currentChar == '"') break;

            inEscape = currentChar == '\\' ? !inEscape : false;
            lastIndex++;
        }

        var subStr = (object)commandString.Substring(startIndex + 1, lastIndex - textStartIndex);
        return ResC.TOk((lastIndex + 1, subStr));
    }

    private static Res<(int NextIndex, object Parameter)> ParseParameterInt(ref string commandString, int startIndex)
    {
        var lastIndex = startIndex;
        if (commandString[lastIndex] == '-')
            lastIndex++;

        while (true)
        {
            if (lastIndex >= commandString.Length)
                return ResC.TFail<(int, object)>($"Failed to find end of integer parameter at index {startIndex}");

            var currentChar = commandString[lastIndex];
            if (currentChar == ' ' || currentChar == ']') break;

            if (currentChar < '0' || currentChar > '9')
                return ResC.TFail<(int, object)>($"Unexpected character '{currentChar}' for integer parameter at index {lastIndex}, should only be numeric");

            lastIndex++;
        }
        
        var len = lastIndex - startIndex;
        if ((commandString[lastIndex] == '-' && len == 1) || len == 0)
            return ResC.TFail<(int, object)>($"Number value length is 0 for integer parameter at {startIndex}");

        var substr = commandString.Substring(startIndex, len);
        if (!int.TryParse(substr, out var res))
            return ResC.TFail<(int, object)>($"Failed to convert integer parameter \"{substr}\" at index {startIndex} into integer");

        return ResC.TOk((lastIndex, (object)res));
    }

    private static Res<(int NextIndex, object Parameter)> ParseParameterFloat(ref string commandString, int startIndex)
    {
        var lastIndex = startIndex;
        if (commandString[lastIndex] == '-')
            lastIndex++;

        var pastDecimal = false;
        while (true)
        {
            if (lastIndex >= commandString.Length)
                return ResC.TFail<(int, object)>($"Failed to find end of float parameter at index {startIndex}");

            var currentChar = commandString[lastIndex];
            if (currentChar == ' ' || currentChar == ']') break;

            if (currentChar == ',' || currentChar == '.')
            {
                if (pastDecimal)
                    return ResC.TFail<(int, object)>($"Unexpected second decimal delimiter in float parameter at index {lastIndex}");

                pastDecimal = true;
            }
            else if (currentChar < '0' || currentChar > '9')
                return ResC.TFail<(int, object)>($"Unexpected character '{currentChar}' for float parameter at index {lastIndex}, should only be numeric");

            lastIndex++;
        }
        
        var len = lastIndex - startIndex ;
        if ((commandString[lastIndex] == '-' && len == 1) || len == 0)
            return ResC.TFail<(int, object)>($"Number value length is 0 for float parameter at {startIndex}");

        var substr = commandString.Substring(startIndex, len);
        if (!float.TryParse(substr, NumberStyles.Float, CultureInfo.InvariantCulture, out var res))
            return ResC.TFail<(int, object)>($"Failed to convert float parameter \"{substr}\" at index {startIndex} into float");

        return ResC.TOk((lastIndex, (object)res));
    }

    private static Res<(int NextIndex, string? Ip, ushort? Port)>? ParseTargetAddress(ref string commandString, int startIndex)
    {
        var ipRes = ParseIpAddress(ref commandString, startIndex);
        if (ipRes is not null && !ipRes.IsOk) return ResC.TFail<(int, string?, ushort?)>(ipRes.Msg);

        var index = ipRes?.Value.NextIndex ?? startIndex;

        var portRes = ParsePort(ref commandString, index);
        if (portRes is not null && !portRes.IsOk) return ResC.TFail<(int, string?, ushort?)>(portRes.Msg);

        if (portRes is null && ipRes is null) return null;
        return ResC.TOk((portRes?.Value.NextIndex ?? index, ipRes?.Value.Ip, portRes?.Value.Port));
    }

    private static Res<(int NextIndex, string Ip)>? ParseIpAddress(ref string commandString, int startIndex)
    {
        if (commandString[startIndex] > '9' || commandString[startIndex] < '0') return null;

        var index = startIndex;
        while(true)
        {
            if (index >= commandString.Length)
                return ResC.TFail<(int, string)>($"Failed to locate end of IP parameter at index {startIndex}");

            var currentChar = commandString[index];
            if (currentChar == ':' || currentChar == ' ' || currentChar == ']') 
                break;

            if (!((currentChar <= '9' && currentChar >= '0') || currentChar == '.'))
                return ResC.TFail<(int, string)>($"Unexpected character '{currentChar}' at index {index} of IP parameter");

            index++;
        }

        var subStr = commandString[startIndex..index];
        if(!subStr.Contains('.') || !IPAddress.TryParse(subStr, out var ip))
            return ResC.TFail<(int, string)>($"Failed to convert IP parameter \"{subStr}\" at index {startIndex} into IP");


        return ResC.TOk((index, ip.ToString()));
    }

    private static Res<(int NextIndex, ushort? Port)>? ParsePort(ref string commandString, int startIndex)
    {
        if (commandString[startIndex] != ':') return null;
        var textStartIndex = startIndex + 1;
        var index = textStartIndex;

        if (textStartIndex < commandString.Length && commandString[textStartIndex] == ']')
            return ResC.TOk<(int, ushort?)>((textStartIndex, null));

        while(true)
        {
            if (index >= commandString.Length)
                return ResC.TFail<(int, ushort?)>($"Failed to locate end of Port parameter at index {startIndex}");

            var currentChar = commandString[index];
            if (currentChar == ' ' || currentChar == ']') 
                break;

            if (currentChar > '9' || currentChar < '0')
                return ResC.TFail<(int, ushort?)>($"Unexpected character '{currentChar}' at index {index} of Port parameter");

            index++;
        }

        var subStr = commandString[textStartIndex..index];
        if (string.IsNullOrEmpty(subStr)) 
            return ResC.TOk<(int, ushort?)>((index, null));

        if(!ushort.TryParse(subStr, result: out var port))
            return ResC.TFail<(int, ushort?)>($"Failed to convert Port parameter \"{subStr}\" at index {startIndex} into Port");

        return ResC.TOk<(int, ushort?)>((index, port));
    }

    private static Res<(int NextIndex, string Target)>? ParseNamedTarget(ref string commandString, int startIndex)
    {
        if (commandString[startIndex] != '"') return null;

        var textStartIndex = startIndex + 1;
        var index = textStartIndex;

        // We do not handle string escapes in this parameter
        while (true)
        {
            if (index >= commandString.Length)
                return ResC.TFail<(int, string)>($"Failed to locate end of named target parameter at index {startIndex}");

            if (commandString[index] == '"') break;
            index++;
        }

        var substr = commandString[textStartIndex..index];
        return ResC.TOk((index + 1, substr));
    }

    private static Res<(int NextIndex, int Wait)>? ParseWait(ref string commandString, int startIndex)
    {
        if (commandString[startIndex] != 'w' && commandString[startIndex] != 'W') return null;

        var textStartIndex = startIndex + 1;
        var index = textStartIndex;

        while(true)
        {
            if (index >= commandString.Length)
                return ResC.TFail<(int, int)>($"Failed to locate end of Wait parameter at index {startIndex}");

            var currentChar = commandString[index];
            if (currentChar == ' ' || currentChar == ']') 
                break;

            if (currentChar > '9' || currentChar < '0')
                return ResC.TFail<(int, int)>($"Unexpected character '{currentChar}' at index {index} of Wait parameter");

            index++;
        }

        var subStr = commandString[textStartIndex..index];
        if(!int.TryParse(subStr, result: out var wait))
            return ResC.TFail<(int, int)>($"Failed to convert Wait parameter \"{subStr}\" at index {startIndex} into Int");

        return ResC.TOk((index, wait));
    }

    private Res<OscCommandInfo> CreateCommandInfo(string address, object[] parameters, string? ip, ushort? port, string? namedTarget, int? wait)
    {
        var ipValue = ip;
        var portValue = port;

        if (namedTarget is not null)
        {
            var host = _hosts.GetServiceAddressByName(namedTarget);
            if (!host.IsOk) return ResC.TFail<OscCommandInfo>(host.Msg);

            ipValue ??= host.Value.Ip;
            portValue ??= host.Value.Port.ConvertToUshort();
        }

        return ResC.TOk(new OscCommandInfo()
        {
            Address = address,
            Arguments = parameters,
            Wait = wait,
            Port = portValue,
            Ip = ipValue
        });
    }
}