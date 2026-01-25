namespace HoscyCore.Services.DependencyCore;

public interface IContainerBulkLoader<T> where T : class, IService
{
    T? GetInstance(Type type);
    IEnumerable<T> GetInstances();
}