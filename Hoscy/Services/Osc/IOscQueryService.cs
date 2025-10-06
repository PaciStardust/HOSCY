using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Osc;

public interface IOscQueryService : IStartStopService
{
    /// <summary>
    /// Returns IP and Port of OscQueryService by name
    /// </summary>
    /// <returns>Null if not found</returns>
    public (string, int)? GetServiceAddressByName(string name);
}