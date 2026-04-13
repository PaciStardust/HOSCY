using HoscyCore.Utility;

namespace HoscyCore.Services.Dependency;

public interface IContainerBulkLoader<T> where T : class
{
    Res<T> GetInstance(Type type);
    Res<List<T>> GetInstances();
}