using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;

namespace HoscyCoreTests.Mocks.Impl;

public class MockContainerBulkLoader<T>(Func<IEnumerable<T>> instanceGenerator) : IContainerBulkLoader<T> where T : class, IService
{
    public Func<IEnumerable<T>> InstanceGenerator { get; set; } = instanceGenerator;

    public Res<T> GetInstance(Type type)
    {
        var inst = InstanceGenerator().FirstOrDefault(x => x.GetType() == type);
        return inst is null ? ResC.TFail<T>(ResMsg.Err("No instance found")) : ResC.TOk(inst);
    }

    public Res<T> GetInstance()
    {
        var instances = GetInstances();
        if (instances.IsOk)
        {
            return instances.Value.Count > 0 ? ResC.TOk(instances.Value[0]) : ResC.TFail<T>("Unable to retrieve instance");
        }
        return ResC.TFail<T>(instances.Msg);
    }

    public Res<List<T>> GetInstances()
    {
        return ResC.TOk(InstanceGenerator().ToList());
    }
}