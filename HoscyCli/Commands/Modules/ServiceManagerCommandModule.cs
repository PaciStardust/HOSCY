using System.Reflection;
using HoscyCli.Commands.Core;
using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(ServiceManagerCommandModule))]
public class ServiceManagerCommandModule : AttributeCommandModule, ICoreCommandModule
{
    private readonly IService[] _services;
    private readonly ILogger _logger;

    public string ModuleName => "Services";
    public string ModuleDescription => "Retrieve service infos";
    public string[] ModuleCommands => ["services", "srv"];

    public ServiceManagerCommandModule(IServiceProvider services, ILogger logger)
    {
        _logger = logger.ForContext<ServiceManagerCommandModule>();
        _services = LaunchUtils.GetImplementationsInContainerForClass<IService>(services, _logger).ToArray();
    }        

    [SubCommandModule(["list", "l", "all", "a"], "List all running services")]
    public CommandResult List()
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
        ServiceStatus? serviceStatus = service is IStartStopService startStopService ? startStopService.GetCurrentStatus() : null;  

        var statusIcon = serviceStatus.HasValue ? serviceStatus.Value switch
        {
            ServiceStatus.Stopped => '~',
            ServiceStatus.Faulted => '!',
            ServiceStatus.Started => '+',
            ServiceStatus.Processing => '*',
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
            if (OnInvalidInt(args, out var idx, 0, _services.Length, "Specified argument not a valid index"))
                return CommandResult.Error;
            
            var selected = _services[idx.Value];
            var startStopService = selected as IStartStopService;
            if (OnTrue(startStopService is null, $"Service {selected.GetType().Name} does not have a status"))
                return CommandResult.Success;

            var serviceEx = startStopService!.GetFaultIfExists();
            if (OnTrue(serviceEx is null, $"Service {selected.GetType().Name} does not have an error"))
                return CommandResult.Success;

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

    [SubCommandModule(["up"], "Starts a service")]
    public CommandResult ServiceUp(string? args)
    {
        if (OnInvalidInt(args, out var validInt, 0, _services.Length, "You must specify a service to start"))
            return CommandResult.MissingParameter;
        var selected = _services[validInt.Value];

        var startStopService = selected as IStartStopService;
        if (OnTrue(startStopService is null, "Selected service does not support starting")) 
            return CommandResult.Error;

        _logger.Debug("Manually starting service: \"{serviceName}\"", selected.GetType().Name);
        startStopService!.Start();
        _logger.Debug("Manually started service: \"{serviceName}\"", selected.GetType().Name);
        return CommandResult.Success;
    }

    [SubCommandModule(["down"], "Stops a service")]
    public CommandResult ServiceDown(string? args)
    {
        if (OnInvalidInt(args, out var validInt, 0, _services.Length, "You must specify a service to stop"))
            return CommandResult.MissingParameter;
        var selected = _services[validInt.Value];

        var startStopService = selected as IStartStopService;
        if (OnTrue(startStopService is null, "Selected service does not support stopping")) 
            return CommandResult.Error;

        _logger.Debug("Manually stopping service: \"{serviceName}\"", selected.GetType().Name);
        startStopService!.Stop();
        _logger.Debug("Manually stopping service: \"{serviceName}\"", selected.GetType().Name);
        return CommandResult.Success;
    }

    [SubCommandModule(["restart"], "Restarts a service")]
    public CommandResult ServiceRestart(string? args)
    {
        if (OnInvalidInt(args, out var validInt, 0, _services.Length, "You must specify a service to restart"))
            return CommandResult.MissingParameter;
        var selected = _services[validInt.Value];

        var startStopService = selected as IStartStopService;
        if (OnTrue(startStopService is null, "Selected service does not support restarting")) 
            return CommandResult.Error;

        _logger.Debug("Manually restarting service: \"{serviceName}\"", selected.GetType().Name);
        startStopService!.Restart();
        _logger.Debug("Manually restarting service: \"{serviceName}\"", selected.GetType().Name);
        return CommandResult.Success;
    }
}