using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslatorManagerService : IAutoStartStopService
{
    #region Info
    public IReadOnlyList<(string ProperName, string TypeName)> GetAvailableNames();
    public string? GetCurrentName();
    #endregion

    #region Start / Stop
    public void StartTranslator(string? name = null, string? typeName = null);
    public void StopCurrentTranslator();
    public void RestartCurrentTranslator();
    public ServiceStatus GetCurrentTranslatorStatus();
    #endregion

    #region Functionality
    public TranslationResult TryTranslate(string input, out string? output);
    #endregion
}