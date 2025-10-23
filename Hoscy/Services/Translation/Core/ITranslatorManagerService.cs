using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Translation.Core;

public interface ITranslatorManagerService : IStartStopService
{
    #region Info
    public IReadOnlyList<(string, string)> GetAvailableNames();
    public string? GetCurrentName();
    #endregion

    #region Start / Stop
    public void StartTranslator(string name);
    public void StopCurrentTranslator();
    public void RestartCurrentTranslator();
    public StartStopStatus GetCurrentTranslatorStatus();
    #endregion

    #region Functionality
    public bool TryTranslate(string input, [NotNullWhen(true)] string? output);
    #endregion
}