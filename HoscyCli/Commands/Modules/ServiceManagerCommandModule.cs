using System.Reflection;
using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ServiceManagerCommandModule))]
public class ServiceManagerCommandModule(IServiceProvider services, ILogger logger) : AttributeCommandModule {
    private readonly IServiceProvider _services = services;
    private readonly ILogger _logger = logger.ForContext<ServiceManagerCommandModule>();

    [SubCommandModule(["list", "l", "all", "a"], "List all running services")]
    public CommandResult List(string? _)
    {
        var allServices = LaunchUtils.GetImplementationsInContainerForClass<IService>(_services, _logger).ToArray();
        var serviceString = GenerateServiceString(allServices);
        Console.WriteLine(serviceString);
        return CommandResult.Success;
    }

    private string GenerateServiceString(IService[] services)
    {
        var i = 0;
        var segments = services.Select(x => $"{i++,-4} {GetServiceInfo(x)}");
        return string.Join("\n", segments);
    }

    private string GetServiceInfo(IService service)
    {
        var serviceType = service.GetType();
        var serviceLoadedForType = serviceType.GetCustomAttribute<LoadIntoDiContainerAttribute>()?.AsType;
        StartStopStatus? serviceStatus = service is IStartStopService startStopService ? startStopService.GetStatus() : null;  

        var statusIcon = serviceStatus.HasValue ? serviceStatus.Value switch
        {
            StartStopStatus.Running => '+',
            StartStopStatus.Stopped => '~',
            StartStopStatus.Faulted => '!',
            _ => '?'
        } : ' ';

        var serviceName = serviceLoadedForType is null || serviceLoadedForType == serviceType
            ? serviceType.Name
            : $"{serviceLoadedForType.Name} => {serviceType.Name}";
        return $"{statusIcon} | {serviceName}";
    }
}