using System.Collections.Generic;
using Hoscy.Configuration.Modern;

namespace Hoscy.Services.Osc.Relay;

public readonly struct OscReadonlyRelayFilter(OscRelayFilterModel model)
{
    public readonly string Name { get; } = model.Name;
    public readonly ushort Port { get; } = model.Port;
    public readonly string Ip { get; } = model.Ip;
    public readonly IReadOnlyList<string> Filters { get; } = model.Filters;
    public readonly bool BlacklistMode { get; } = model.BlacklistMode;

    /// <summary>
    /// Checks if an address matches the filters filters
    /// </summary>
    /// <param name="input">Address to check</param>
    /// <returns>Matches?</returns>
    public bool Matches(string input)
    {
        if (Filters.Count == 0)
            return true;

        foreach (var filter in Filters)
            if (input.StartsWith(filter))
                return !BlacklistMode;

        return BlacklistMode;
    }

    public override string ToString()
        => $"{Name} ={(BlacklistMode ? "B" : string.Empty)}> {Ip}:{Port}";
}