namespace HoscyCore.Services.Core;

public interface IModuleStartInfo : IService
{
    public Type ModuleType { get; }
}

/// <summary>
/// ModuleStartInfo for controllers with singular enabled modules
/// </summary>
public interface ISoloModuleStartInfo : IModuleStartInfo
{
    public string Name { get; }
    public string Description { get; }
}

/// <summary>
/// ModuleStartInfo for controllers with multiple enabled modules
/// </summary>
public interface IMultiModuleStartInfo : IModuleStartInfo
{
    public bool ShouldBeEnabled();
}