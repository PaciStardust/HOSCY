namespace Hoscy.Services.Osc.Misc;

public record OscCommandInfo()
{
    public required string Address;
    public required object[] Arguments;
    public string? Ip;
    public int? Port;
    public int? Wait;
}