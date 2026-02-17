namespace HoscyCore.Services.Core;

public interface IContainerBulkLoader<T> where T : class
{
    T? GetInstance(Type type);
    IEnumerable<T> GetInstances();
}