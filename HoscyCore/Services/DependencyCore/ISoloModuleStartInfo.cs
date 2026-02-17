namespace HoscyCore.Services.DependencyCore;

public interface ISoloModuleStartInfo : IService
{
    public string Name { get; }
    public string Description { get; }
    public Type ModuleType { get; }
}