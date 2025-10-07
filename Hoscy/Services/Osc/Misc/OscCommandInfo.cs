using System.Linq;

namespace Hoscy.Services.Osc.Misc;

public record OscCommandInfo()
{
    public required string Address;
    public required object[] Arguments;
    public string? Ip;
    public ushort? Port;
    public int? Wait;

    override public string ToString()
    {
        return $"{Address} ({string.Join(",", Arguments.Select(x => x.ToString()))}) => {Ip ?? "NULL"}:{Port?.ToString() ?? "NULL"}";
    }
}