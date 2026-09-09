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
    public Task HandleMessage(string contents, string source);
    public Task HandleNotification(string contents, string source, OutputNotificationPriority priority);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion
}