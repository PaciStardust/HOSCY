using HoscyCore.Utility;

namespace HoscyCore.Services.Core;

/// <summary>
/// Represents a service that can be started and stopped
/// </summary>
public interface IStartStopService : IService
{
    public Res Start();
    public Res Stop();
    public Res Restart();
    public ServiceStatus GetCurrentStatus();
    public ResMsg? GetErrorMessageIfExists();
}

/// <summary>
/// Represents a StartStopService that will be launched automatically
/// </summary>
public interface IAutoStartStopService : IStartStopService;