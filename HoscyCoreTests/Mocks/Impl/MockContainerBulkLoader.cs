using HoscyCore.Services.Core;

namespace HoscyCoreTests.Mocks.Impl;

public class MockContainerBulkLoader<T>(Func<IEnumerable<T>> instanceGenerator) : IContainerBulkLoader<T> where T : class, IService
{
    private readonly Func<IEnumerable<T>> _instanceGenerator = instanceGenerator;

    public T? GetInstance(Type type)
    {
        return _instanceGenerator().FirstOrDefault(x => x.GetType() == type);
    }

    public IEnumerable<T> GetInstances()
    {
        return _instanceGenerator();
    }
}