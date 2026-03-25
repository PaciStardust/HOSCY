using HoscyCli.Commands.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Output.Core;
using HoscyCore.Services.Recognition.Core;
using Serilog;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(LiveCommandModule))]
public class LiveCommandModule(IRecognitionManagerService recognition, IOutputManagerService output, ILogger logger) : AttributeCommandModule, ICoreCommandModule
{
    #region Vars
    private readonly IRecognitionManagerService _recognition = recognition;
    private readonly IOutputManagerService _output = output;
    private readonly ILogger _logger = logger.ForContext<LiveCommandModule>();
    #endregion

    #region Core Info
    public string ModuleName => "Live Info";
    public string ModuleDescription => "Live information for output and control";
    public string[] ModuleCommands => [ "live", "control" ];
    #endregion

    #region Functionality
    [SubCommandModule(["start", "run"], "StartDo partial replacements Live")]
    public CommandResult CmdStart()
    {
        _logger.Information("Entering LIVE");

        _sendCount = 0;

        _output.OnClear += OnClear;
        _output.OnMessage += OnMessage;
        _output.OnNotification += OnNotification;
        _recognition.OnModuleStatusChanged += OnModuleStatusChanged;

        SendReminder("LIVE started");

        while (true)
        {
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.E) break;
            HandleKey(key.Key);
        }

        _recognition.OnModuleStatusChanged -= OnModuleStatusChanged;
        _output.OnNotification -= OnNotification;
        _output.OnMessage -= OnMessage;
        _output.OnClear -= OnClear;

        _logger.Information("Exiting LIVE");
        return CommandResult.Success;
    }

    private void HandleKey(ConsoleKey key)
    {
        switch(key)
        {
            case ConsoleKey.M:
                _logger.Information("Requested mute state toggle");
                Console.WriteLine("Mute toggle requested...");
                _recognition.SetListening(!_recognition.IsListening);
                return;

            case ConsoleKey.S:
                _logger.Information("Requested recognition state toggle");
                Console.WriteLine("Recognition toggle requested...");
                if (_recognition.GetCurrentModuleInfo() is null)
                    _recognition.StartModule();
                else
                    _recognition.StopModule();
                return;

            default:
                return;
        }
    }

    private void OnModuleStatusChanged(object? _, RecognitionStatusChangedEventArgs __)
    {
        SendReminder("Recognition status changed");
    }

    private void OnNotification(object? _, OutputNotificationEventArgs e)
    {
        var outputs = e.Outputs.Length == 0 
            ? "Nothing"
            : string.Join(", ", e.Outputs);

        SendLive($"NOTIF ({e.Priority}, via {outputs}) => {e.Contents}");
    }

    private void OnMessage(object? _, OutputMessageEventArgs e)
    {
        var outputs = e.Outputs.Length == 0 
            ? "Nothing"
            : string.Join(", ", e.Outputs);

        var content = e.Translation is null
            ? e.Contents
            : $"{e.Translation} ({e.Outputs})";

        SendLive($"MESSAGE (via {outputs}) => {content}");
    }

    private void OnClear(object? _, EventArgs __)
    {
        SendLive("CLEARED");
    }

    private uint _sendCount = 0;
    private void SendLive(string text)
    {
        Console.WriteLine($"LIVE: {text}");

        if (++_sendCount % 8 == 0)
        {
            SendReminder(null);
        }
    }

    private void SendReminder(string? extra)
    {
        var text = extra is null ? string.Empty : $"\n{extra}";
        var info = $"Rec={_recognition.GetCurrentModuleInfo()?.Name ?? "None"} Status={_recognition.GetCurrentStatus()} Listen={_recognition.IsListening}";

        Console.WriteLine($"\n\n + + + You are currently in LIVE Mode + + + \n > {info}\n\n > Key Commands: E=Exit, S=Start/Stop, M=Mute\n{text}\n");
    }
    #endregion
}