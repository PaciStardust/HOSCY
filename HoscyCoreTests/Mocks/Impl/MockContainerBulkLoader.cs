using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Impl;

public class MockContainerBulkLoader<T>(Func<IEnumerable<T>> instanceGenerator) : IContainerBulkLoader<T> where T : class, IService
{
    private readonly Func<IEnumerable<T>> _instanceGenerator = instanceGenerator;

    public Res<T> GetInstance(Type type)
    {
        var inst = _instanceGenerator().FirstOrDefault(x => x.GetType() == type);
        return inst is null ? ResC.TFail<T>(ResMsg.Err("No instance found")) : ResC.TOk(inst);
    }

    public Res<List<T>> GetInstances()
    {
        return ResC.TOk(_instanceGenerator().ToList());
    }
}