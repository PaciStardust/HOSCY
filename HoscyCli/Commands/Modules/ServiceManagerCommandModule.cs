using System.Reflection;
using HoscyCli.Commands.Core;
using HoscyCore.Services.DependencyCore;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ServiceManagerCommandModule))]
public class ServiceManagerCommandModule : AttributeCommandModule {
    private readonly IService[] _services;
    private readonly ILogger _logger;
    public ServiceManagerCommandModule(IServiceProvider services, ILogger logger)
    {
        _logger = logger.ForContext<ServiceManagerCommandModule>();
        _services = LaunchUtils.GetImplementationsInContainerForClass<IService>(services, _logger).ToArray();
    }        

    [SubCommandModule(["list", "l", "all", "a"], "List all running services")]
    public CommandResult List(string? _)
    {
        var serviceString = GenerateServiceString(_services);
        Console.WriteLine(serviceString);
        return CommandResult.Success;
    }

    private static string GenerateServiceString(IService[] services)
    {
        var i = 0;
        var segments = services.Select(x => $"{i++,-4} {GetServiceInfo(x)}");
        return string.Join("\n", segments);
    }

    private static string GetServiceInfo(IService service)
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

    [SubCommandModule(["errors"], "List service errors")]
    public CommandResult Errors(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            var startStopServices = _services.Select(x => x as IStartStopService);
            var exceptions = startStopServices.Select(x => x?.GetFaultIfExists());

            var idx = -1;
            var exceptionStrings = exceptions.Select(x =>
            {
                idx++;
                if (x is null) return null;
                return  $"{idx,-4} | {_services[idx].GetType().Name} => {x.GetType().Name}: {x.Message}";
            });
            var actualStrings = exceptions.Where(x => x != null).ToArray();

            if (actualStrings.Length == 0)
            {
                Console.WriteLine("No services with errors found");
            }
            else
            {
                Console.WriteLine($"All errors:\n{string.Join("\n", actualStrings)}");
            }
        } 
        else
        {
            if (!IsValidInteger(args, out var idx, 0, _services.Length, "Specified argument not a valid index")) return CommandResult.Error;
            
            var selected = _services[idx.Value];
            if (selected is not IStartStopService startStopService)
            {
                Console.WriteLine($"Service {selected.GetType().Name} does not have a status");
                return CommandResult.Success;
            }
            var serviceEx = startStopService.GetFaultIfExists();
            if (serviceEx is null)
            {
                Console.WriteLine($"Service {selected.GetType().Name} does not have an error");
                return CommandResult.Success;
            }

            var exceptions = new List<Exception>();
            while (serviceEx != null)
            {
                exceptions.Add(serviceEx);
                serviceEx = serviceEx.InnerException;
            }

            var exStrings = exceptions.Select(x => $"{x.GetType().Name} -> {x.Message}:\n{x.StackTrace}");
            var message = $"Errors from {selected.GetType().Name}:\n\n{string.Join("\n\n--------------\n\n", exStrings)}";
            Console.WriteLine(message);
        }
        return CommandResult.Success;
    }
}