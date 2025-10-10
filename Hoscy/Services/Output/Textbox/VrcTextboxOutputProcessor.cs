using System;
using System.Collections.Generic;
using Hoscy.Configuration.Modern;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Osc.SendReceive;
using Hoscy.Services.Output.Core;
using Serilog;

namespace Hoscy.Services.Output.Textbox;

[LoadIntoDiContainer(typeof(VrcTextboxOutputProcessor), Lifetime.Transient)]
public class VrcTextboxOutputProcessor(ILogger logger, ConfigModel config, IOscSendService sender) : IOutputProcessor
{
    #region Injected Services
    private readonly ILogger _logger = logger.ForContext<VrcTextboxOutputProcessor>();
    private readonly ConfigModel _config = config;
    private readonly IOscSendService _sender = sender;
    #endregion

    #region Processor Variables
    private (string, OutputNotificationPriority)? _currentNotification = null;
    private readonly Queue<string> _currentMessages = [];
    private const int TIMEOUT_MINIMUM_MS = 1250;
    private bool _isClearPending = false;
    private DateTime _intendedTimeoutUntil = DateTime.MinValue;
    private bool _lastSetProcessingState = false;
    #endregion

    #region Events
    public event EventHandler<Exception> OnRuntimeError = delegate { };
    public event EventHandler OnShutdownCompleted = delegate { };
    #endregion

    #region Information
    public OutputProcessorInfo GetInfo()
        => _info;

    private readonly OutputProcessorInfo _info = new()
    {
        Name = "VRChat Textbox",
        Description = "Sends Output to the VRChat Textbox via OSC",
        Flags = OutputProcessorInfoFlags.SupportsMessages | OutputProcessorInfoFlags.SupportsNotifications | OutputProcessorInfoFlags.SupportsProcessingIndicator,
        ProcessorType = typeof(VrcTextboxOutputProcessor)
    };
    #endregion

    public void Activate()
    {
        throw new NotImplementedException();
    }

    public bool Clear()
    {
        throw new NotImplementedException();
    }

    public Exception? GetFaultIfExists()
    {
        throw new NotImplementedException();
    }

    public StartStopStatus GetStatus()
    {
        throw new NotImplementedException();
    }

    public bool IsRunning()
    {
        throw new NotImplementedException();
    }

    public void Restart()
    {
        throw new NotImplementedException();
    }

    public bool ProcessMessage(string contents)
    {
        throw new NotImplementedException();
    }

    public bool ProcessNotification(string contents, OutputNotificationPriority priority)
    {
        throw new NotImplementedException();
    }

    public bool SetProcessingIndicator(bool isProcessing)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
    {
        throw new NotImplementedException();
    }
}