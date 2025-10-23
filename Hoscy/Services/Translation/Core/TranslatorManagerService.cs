using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hoscy.Services.DependencyCore;
using Hoscy.Services.Interfacing;
using Serilog;

namespace Hoscy.Services.Translation.Core;

[LoadIntoDiContainer(typeof(ITranslatorManagerService), Lifetime.Singleton)]
public class TranslatorManagerService(IBackToFrontNotifyService notify, ILogger logger, IServiceProvider services) : StartStopServiceBase, ITranslatorManagerService
{
    #region Injected
    private readonly IBackToFrontNotifyService _notify = notify;
    private readonly ILogger _logger = logger.ForContext<TranslatorManagerService>();
    private readonly IServiceProvider _services = services;
    #endregion

    #region Service Vars
    private readonly List<(string, Type)> _availableTranslators = [];
    private ITranslator? _currentTranslator = null;
    #endregion

    #region Info
    public IReadOnlyList<string> GetAvailableNames()
        => _availableTranslators.Select(x => x.Item1).ToList();

    public string? GetCurrentName()
        => _currentTranslator?.GetName();
    #endregion

    #region Start / Stop
    protected override void StartInternal()
    {
        throw new NotImplementedException();
    }

    public override void Stop()
    {
        throw new NotImplementedException();
    }

    public override void Restart()
    {
        throw new NotImplementedException();
    }

    public override bool IsRunning()
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Translator => Start / Stop
    public void StartTranslator(string name)
    {
        throw new NotImplementedException();
    }

    public void StopCurrentTranslator()
    {
        throw new NotImplementedException();
    }

    public void RestartCurrentTranslator()
    {
        throw new NotImplementedException();
    }

    public StartStopStatus GetCurrentTranslatorStatus()
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Translator => Functionality
    public bool TryTranslate(string input, [NotNullWhen(true)] string? output)
    {
        throw new NotImplementedException();
    }
    #endregion
}