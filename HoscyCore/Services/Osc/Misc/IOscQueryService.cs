using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Osc.Misc;

public interface IOscQueryService : IAutoStartStopService
{
    /// <summary>
    /// Returns IP and Port of OscQueryService by name
    /// </summary>
    /// <returns>Null if not found</returns>
    public (string Ip, int Port)? GetServiceAddressByName(string name);
}