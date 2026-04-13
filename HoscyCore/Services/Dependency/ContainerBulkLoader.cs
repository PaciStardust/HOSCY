using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Dependency;

[LoadIntoDiContainer(typeof(IContainerBulkLoader<>))]
public class ContainerBulkLoader<T>(IServiceProvider serviceProvider, ILogger logger) : IContainerBulkLoader<T> where T : class
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger _logger = logger.ForContext(typeof(ContainerBulkLoader<>));

    public Res<List<T>> GetInstances()
    {
        return LaunchUtils.GetImplementationsInContainerForClass<T>(_serviceProvider, _logger);
    }

    public Res<T> GetInstance(Type type)
    {
        var instance = _serviceProvider.GetService(type);
        if (instance is T correctType)
        {
            _logger.Debug("Retrieved instance of type {type} for requested type {requestedType}",
                instance.GetType().FullName, type.FullName);
            return ResC.TOk(correctType);
        }

        _logger.Warning("Failed to retrieve instance of type \"{type}\"", type.FullName);
        return ResC.TFail<T>(ResMsg.Err($"Failed to retrieve instance of type \"{type.Name}\""));
    }
}