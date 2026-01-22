using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using Serilog;

[LoadIntoDiContainer(typeof(ContainerBulkLoader<>))]
public class ContainerBulkLoader<T>(IServiceProvider serviceProvider, ILogger logger) where T : class, IService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger _logger = logger.ForContext<ContainerBulkLoader<T>>();

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