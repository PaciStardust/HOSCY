using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Dependency;

[LoadIntoDiContainer(typeof(IContainerBulkLoader<>))]
public class ContainerBulkLoader<T>(IServiceProvider serviceProvider, ILogger logger) : IContainerBulkLoader<T> where T : class
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger _logger = logger.ForContext(typeof(ContainerBulkLoader<>));

    public IEnumerable<T> GetInstances()
    {
        return LaunchUtils.GetImplementationsInContainerForClass<T>(_serviceProvider, _logger).ToArray();
    }

    public T? GetInstance(Type type)
    {
        var instance = _serviceProvider.GetService(type);
        return instance is T correctType ? correctType : null;
    }
}