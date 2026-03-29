using HoscyCore.Services.Core;

namespace HoscyCore.Services.Output.Core;

public interface IOutputHandlerStartInfo : IMultiModuleStartInfo;

public interface IOutputHandler : IStartStopModule
{
    #region Info
    public string Name { get; }
    public OutputTranslationFormat GetTranslationOutputMode();
    public OutputsAsMediaFlags OutputTypeFlags { get; }
    #endregion

    #region Functionality
    public void HandleMessage(string contents);
    public void HandleNotification(string contents, OutputNotificationPriority priority);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion
}