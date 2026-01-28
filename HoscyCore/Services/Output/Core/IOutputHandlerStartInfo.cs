using HoscyCore.Services.DependencyCore;
using Serilog;

namespace HoscyCore.Services.Output.Core;

/// <summary>
/// Defines information for starting Processors
/// </summary>
public interface IOutputHandlerStartInfo : IService
{
    public Type HandlerType { get; }
    public bool ShouldBeEnabled();    
}